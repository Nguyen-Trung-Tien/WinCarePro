using System;
using System.Collections.Generic;

namespace WinCarePro.Core.Models;

/// <summary>
/// Status classification for all operations across WinCare Pro.
/// </summary>
public enum OperationStatus
{
    Success,
    PartialSuccess,
    Warning,
    Cancelled,
    Failed,
    RequiresElevation,
    RequiresRestart
}

/// <summary>
/// Standard operation result representing the outcome of a system action.
/// </summary>
public class OperationResult
{
    public OperationStatus Status { get; set; } = OperationStatus.Success;
    public bool IsSuccess => Status == OperationStatus.Success || Status == OperationStatus.PartialSuccess;
    public bool IsFailure => !IsSuccess;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? ErrorCode { get; set; }
    public Exception? Exception { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool HasWarnings => Warnings.Count > 0;
    public bool HasErrors => Errors.Count > 0;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public static OperationResult Ok(string message = "Operation completed successfully.", TimeSpan duration = default)
    {
        return new OperationResult
        {
            Status = OperationStatus.Success,
            Message = message,
            Duration = duration
        };
    }

    public static OperationResult WarningResult(string message, IEnumerable<string>? warnings = null, TimeSpan duration = default)
    {
        var result = new OperationResult
        {
            Status = OperationStatus.Warning,
            Message = message,
            Duration = duration
        };
        if (warnings != null)
        {
            result.Warnings.AddRange(warnings);
        }
        return result;
    }

    public static OperationResult Partial(string message, IEnumerable<string>? warnings = null, TimeSpan duration = default)
    {
        var result = new OperationResult
        {
            Status = OperationStatus.PartialSuccess,
            Message = message,
            Duration = duration
        };
        if (warnings != null)
        {
            result.Warnings.AddRange(warnings);
        }
        return result;
    }

    public static OperationResult Fail(string message, string? errorCode = null, string? details = null, TimeSpan duration = default, IEnumerable<string>? errors = null)
    {
        return Fail(message, (Exception?)null, errorCode, details, duration, errors);
    }

    public static OperationResult Fail(string message, Exception? ex, string? errorCode = null, string? details = null, TimeSpan duration = default, IEnumerable<string>? errors = null)
    {
        var res = new OperationResult
        {
            Status = OperationStatus.Failed,
            Message = message,
            ErrorCode = errorCode ?? ex?.GetType().Name,
            Details = details ?? ex?.Message,
            Exception = ex,
            Duration = duration
        };
        if (!string.IsNullOrEmpty(res.ErrorCode))
        {
            res.Errors.Add(res.ErrorCode);
        }
        if (errors != null)
        {
            res.Errors.AddRange(errors);
        }
        return res;
    }

    public static OperationResult CancelledResult(string message = "Operation was cancelled by user.", TimeSpan duration = default)
    {
        return new OperationResult
        {
            Status = OperationStatus.Cancelled,
            Message = message,
            Duration = duration
        };
    }

    public static OperationResult ElevationRequired(string message = "Administrator privileges are required for this operation.")
    {
        return new OperationResult
        {
            Status = OperationStatus.RequiresElevation,
            Message = message
        };
    }

    public static OperationResult RestartRequired(string message = "System restart is required for changes to take effect.")
    {
        return new OperationResult
        {
            Status = OperationStatus.RequiresRestart,
            Message = message
        };
    }

    public static OperationResult Combine(params OperationResult[] results)
    {
        var combined = new OperationResult();
        foreach (var r in results)
        {
            if (r.Status == OperationStatus.Failed)
            {
                combined.Status = OperationStatus.Failed;
            }
            else if (r.Status == OperationStatus.PartialSuccess && combined.Status == OperationStatus.Success)
            {
                combined.Status = OperationStatus.PartialSuccess;
            }
            combined.Warnings.AddRange(r.Warnings);
            combined.Errors.AddRange(r.Errors);
        }
        return combined;
    }
}

/// <summary>
/// Generic operation result carrying typed data payload.
/// </summary>
public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }

    public static implicit operator OperationResult<T>(T data) => Ok(data);

    public static OperationResult<T> Ok(T data, string message = "Operation completed successfully.", TimeSpan duration = default)
    {
        return new OperationResult<T>
        {
            Status = OperationStatus.Success,
            Message = message,
            Data = data,
            Duration = duration
        };
    }

    public static new OperationResult<T> Fail(string message, string? errorCode = null, string? details = null, TimeSpan duration = default, IEnumerable<string>? errors = null)
    {
        return Fail(message, (Exception?)null, errorCode, details, duration, errors);
    }

    public static new OperationResult<T> Fail(string message, Exception? ex, string? errorCode = null, string? details = null, TimeSpan duration = default, IEnumerable<string>? errors = null)
    {
        var res = new OperationResult<T>
        {
            Status = OperationStatus.Failed,
            Message = message,
            ErrorCode = errorCode ?? ex?.GetType().Name,
            Details = details ?? ex?.Message,
            Exception = ex,
            Duration = duration,
            Data = default
        };
        if (!string.IsNullOrEmpty(res.ErrorCode))
        {
            res.Errors.Add(res.ErrorCode);
        }
        if (errors != null)
        {
            res.Errors.AddRange(errors);
        }
        return res;
    }

    public static new OperationResult<T> CancelledResult(string message = "Operation was cancelled by user.", TimeSpan duration = default)
    {
        return new OperationResult<T>
        {
            Status = OperationStatus.Cancelled,
            Message = message,
            Duration = duration,
            Data = default
        };
    }

    public static OperationResult<T> Partial(T data, string message, IEnumerable<string>? warnings = null, TimeSpan duration = default)
    {
        var result = new OperationResult<T>
        {
            Status = OperationStatus.PartialSuccess,
            Message = message,
            Data = data,
            Duration = duration
        };
        if (warnings != null)
        {
            result.Warnings.AddRange(warnings);
        }
        return result;
    }
}
