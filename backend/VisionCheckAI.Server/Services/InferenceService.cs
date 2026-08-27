using System.Diagnostics;
using System.Text.Json;
using VisionCheckAI.Server.Models;

namespace VisionCheckAI.Server.Services;

public interface IInferenceService
{
    Task<InferenceResult> RunInferenceAsync(string imageAbsolutePath);
}

public class InferenceResult
{
    public string Prediction { get; set; } = "Excellent";
    public double Confidence { get; set; } = 0.95;
    public Dictionary<string, double> Probabilities { get; set; } = new();
}

public class PyTorchInferenceService : IInferenceService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PyTorchInferenceService> _logger;

    public PyTorchInferenceService(IWebHostEnvironment env, ILogger<PyTorchInferenceService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<InferenceResult> RunInferenceAsync(string imageAbsolutePath)
    {
        var rootDir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", ".."));
        var scriptPath = Path.Combine(rootDir, "ai_engine", "predict.py");

        if (!File.Exists(scriptPath))
        {
            throw new InvalidOperationException($"AI inference script not found at {scriptPath}.");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ResolvePythonExecutable(rootDir),
                Arguments = $"\"{scriptPath}\" \"{imageAbsolutePath}\" --json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = rootDir
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                _logger.LogError("Python process failed with code {ExitCode}. Error: {Error}", process.ExitCode, error);
                throw new InvalidOperationException("The AI inference process failed.");
            }

            var result = JsonSerializer.Deserialize<InferenceResult>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? throw new InvalidOperationException("The AI inference process returned an empty result.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke python predict.py process");
            throw new InvalidOperationException("AI inference is unavailable. Check the Python environment and model files.", ex);
        }
    }

    private static string ResolvePythonExecutable(string rootDir)
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(rootDir, ".venv", "Scripts", "python.exe"),
                "python"
            }
            : new[]
            {
                Path.Combine(rootDir, ".venv", "bin", "python"),
                Path.Combine(rootDir, ".venv", "bin", "python3"),
                "python3",
                "python"
            };

        foreach (var candidate in candidates)
        {
            if (!Path.IsPathFullyQualified(candidate) || File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No Python executable was found. Create the project virtual environment and install requirements.txt.");
    }
}
