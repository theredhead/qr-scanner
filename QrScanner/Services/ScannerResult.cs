namespace QrScanner.Services;

public sealed class ScannerResult
{
    private ScannerResult(
        bool isSuccess,
        string? rawText,
        string? strategyId,
        string? strategyName,
        string? codeType,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        RawText = rawText;
        StrategyId = strategyId;
        StrategyName = strategyName;
        CodeType = codeType;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public string? RawText { get; }

    public string? StrategyId { get; }

    public string? StrategyName { get; }

    public string? CodeType { get; }

    public string? ErrorMessage { get; }

    public static ScannerResult Success(string rawText, string strategyId, string strategyName, string codeType) =>
        new(true, rawText, strategyId, strategyName, codeType, null);

    public static ScannerResult Failure(string errorMessage) =>
        new(false, null, null, null, null, errorMessage);
}
