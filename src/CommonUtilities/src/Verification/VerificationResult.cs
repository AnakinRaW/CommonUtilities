using System;

namespace AnakinRaW.CommonUtilities.Verification;

/// <summary>
/// The result of a data verification.
/// </summary>
public sealed class VerificationResult : IEquatable<VerificationResult>
{
    /// <summary>
    /// Represents a verification result that indicates the data was not verified.
    /// </summary>
    /// <remarks>This result shall only be used for the <see cref="IVerificationManager"/>.</remarks>
    public static readonly VerificationResult NotVerified = new(VerificationResultStatus.NotVerified);
    /// <summary>
    /// Represents a verification result that indicates the data was verified successfully.
    /// </summary>
    public static readonly VerificationResult Success = new(VerificationResultStatus.Success);
    /// <summary>
    /// Represents a verification result that indicates the data verification failed.
    /// </summary>
    public static readonly VerificationResult Failed = new(VerificationResultStatus.VerificationFailed);
    /// <summary>
    /// Represents a verification result that indicates the verification context was invalid.
    /// </summary>
    public static readonly VerificationResult InvalidContext = new(VerificationResultStatus.VerificationContextError);

    /// <summary>
    /// Gets the status of the verification result.
    /// </summary>
    public VerificationResultStatus Status { get; }

    /// <summary>
    /// Gets the message associated with the verification result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationResult"/> class with the specified status.
    /// </summary>
    /// <param name="status">The status of the verification result.</param>
    internal VerificationResult(VerificationResultStatus status)
    {
        Status = status;
    }

    /// <summary>
    /// Creates a new <see cref="VerificationResult"/> instance with <see cref="Status"/> set to <see cref="VerificationResultStatus.Exception"/>
    /// and the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of the <see cref="VerificationResult"/> class with the specified error message.</returns>
    public static VerificationResult FromError(string? message)
    {
        return new VerificationResult(VerificationResultStatus.Exception)
        {
            Message = message
        };
    }

    /// <inheritdoc/>
    public bool Equals(VerificationResult? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Status == other.Status;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is VerificationResult other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return (int)Status;
    }
}