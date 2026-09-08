using System.Collections.Generic;
using ZXing;

namespace ScannerZero.Services;

public static class ScannerStrategies
{
    public static IReadOnlyList<IScannerStrategy> All { get; } =
    [
        new QrCodeScanStrategy(),
        new DataMatrixScanStrategy(),
        new AztecScanStrategy(),
        new Pdf417ScanStrategy(),
        new Code128ScanStrategy(),
        new Code39ScanStrategy(),
        new Code93ScanStrategy(),
        new Ean13ScanStrategy(),
        new Ean8ScanStrategy(),
        new UpcAScanStrategy(),
        new UpcEScanStrategy(),
        new ItfScanStrategy(),
        new CodabarScanStrategy()
    ];
}

public sealed class QrCodeScanStrategy() : ZxingBarcodeScanStrategy("qr-code", "QR Code", "QR Code", BarcodeFormat.QR_CODE);

public sealed class DataMatrixScanStrategy() : ZxingBarcodeScanStrategy("data-matrix", "Data Matrix", "Data Matrix", BarcodeFormat.DATA_MATRIX);

public sealed class AztecScanStrategy() : ZxingBarcodeScanStrategy("aztec", "Aztec", "Aztec Code", BarcodeFormat.AZTEC);

public sealed class Pdf417ScanStrategy() : ZxingBarcodeScanStrategy("pdf417", "PDF417", "PDF417", BarcodeFormat.PDF_417);

public sealed class Code128ScanStrategy() : ZxingBarcodeScanStrategy("code-128", "Code 128", "Code 128", BarcodeFormat.CODE_128);

public sealed class Code39ScanStrategy() : ZxingBarcodeScanStrategy("code-39", "Code 39", "Code 39", BarcodeFormat.CODE_39);

public sealed class Code93ScanStrategy() : ZxingBarcodeScanStrategy("code-93", "Code 93", "Code 93", BarcodeFormat.CODE_93);

public sealed class Ean13ScanStrategy() : ZxingBarcodeScanStrategy("ean-13", "EAN-13", "EAN-13", BarcodeFormat.EAN_13);

public sealed class Ean8ScanStrategy() : ZxingBarcodeScanStrategy("ean-8", "EAN-8", "EAN-8", BarcodeFormat.EAN_8);

public sealed class UpcAScanStrategy() : ZxingBarcodeScanStrategy("upc-a", "UPC-A", "UPC-A", BarcodeFormat.UPC_A);

public sealed class UpcEScanStrategy() : ZxingBarcodeScanStrategy("upc-e", "UPC-E", "UPC-E", BarcodeFormat.UPC_E);

public sealed class ItfScanStrategy() : ZxingBarcodeScanStrategy("itf", "ITF", "ITF", BarcodeFormat.ITF);

public sealed class CodabarScanStrategy() : ZxingBarcodeScanStrategy("codabar", "Codabar", "Codabar", BarcodeFormat.CODABAR);
