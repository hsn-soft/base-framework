namespace Edx.Shared.Efatura;

/// <summary>
/// The codes of the center (Ek-2 and Ek-3 of the e-Fatura guides): the status of an envelope, which a sender reads in the system response and with <c>getApplicationResponse</c>, and the codes of the faults of <c>sendDocument</c> and <c>getApplicationResponse</c>. <c>1000</c> and <c>1100</c>
/// mean wait, the codes between 1100 and 1200 mean the envelope was refused by the analysis (the sender corrects it and sends a new one), <c>1200</c> and over mean the center took it, and <c>1300</c> is the end of the road.
/// </summary>
public static class EfaturaStatus
{
    public const int Queued = 1000;
    public const int Processing = 1100;

    public const int NotAZip = 1110;
    public const int EnvelopeIdLengthInvalid = 1111;
    public const int NotCopiedFromArchive = 1120;
    public const int NotUnpacked = 1130;
    public const int ZipMustHoldOneFile = 1131;
    public const int NotXml = 1132;
    public const int EnvelopeIdAndXmlNameDiffer = 1133;
    public const int DocumentNotParsed = 1140;
    public const int EnvelopeIdMissing = 1141;
    public const int EnvelopeIdAndZipNameDiffer = 1142;
    public const int VersionInvalid = 1143;
    public const int SchematronFailed = 1150;
    public const int SchemaFailed = 1160;
    public const int SignatureOwnerNotFound = 1161;
    public const int SignatureNotKept = 1162;
    public const int InvoiceAlreadyKnown = 1163;
    public const int DocumentAlreadyKnown = 1164;
    public const int AuthorityNotChecked = 1170;
    public const int SenderUnitNotAuthorized = 1171;
    public const int PostBoxNotAuthorized = 1172;
    public const int SignatureAuthorityNotChecked = 1175;
    public const int SignatureOwnerNotAuthorized = 1176;
    public const int SignatureInvalid = 1177;
    public const int AddressNotChecked = 1180;
    public const int AddressNotFound = 1181;
    public const int UserNotAdded = 1182;
    public const int UserNotRemoved = 1183;
    public const int SystemResponseNotMade = 1190;
    public const int SystemError = 1195;

    public const int Processed = 1200;
    public const int NotDeliveredRetrying = 1210;
    public const int DeliveryFailed = 1215;
    public const int NoSystemResponse = 1220;
    public const int SystemResponseFailed = 1230;
    public const int InvoiceCancelled = 1235;
    public const int Completed = 1300;

    // the faults of sendDocument and getApplicationResponse (before an envelope is queued)
    public const int HashesDiffer = 2000;
    public const int EnvelopeAlreadyThere = 2001;
    public const int NotArchived = 2002;
    public const int NotQueued = 2003;
    public const int EnvelopeNotFound = 2004;
    public const int SystemFault = 2005;
    public const int InvalidEnvelopeName = 2006;
    public const int SendingBlocked = 2007;

    private static readonly Dictionary<int, string> s_names = new()
    {
        [Queued] = "ZARF KUYRUGA EKLENDI",
        [Processing] = "ZARF ISLENIYOR",
        [NotAZip] = "ZIP DOSYASI DEGIL",
        [EnvelopeIdLengthInvalid] = "ZARF ID UZUNLUGU GECERSIZ",
        [NotCopiedFromArchive] = "ZARF ARSIVDEN_KOPYALANAMADI",
        [NotUnpacked] = "ZIP ACILAMADI",
        [ZipMustHoldOneFile] = "ZIP BIR DOSYA ICERMELI",
        [NotXml] = "XML DOSYASI DEGIL",
        [EnvelopeIdAndXmlNameDiffer] = "ZARF ID VE XML DOSYASININ ADI AYNI OLMALI",
        [DocumentNotParsed] = "DOKUMAN AYRISTIRILAMADI",
        [EnvelopeIdMissing] = "ZARF ID YOK",
        [EnvelopeIdAndZipNameDiffer] = "ZARF ID VE ZIP DOSYASI ADI AYNI OLMALI",
        [VersionInvalid] = "GECERSIZ VERSIYON",
        [SchematronFailed] = "SCHEMATRON KONTROL SONUCU HATALI",
        [SchemaFailed] = "XML SEMA KONTROLUNDEN GECEMEDI",
        [SignatureOwnerNotFound] = "IMZA SAHIBI TCKN VKN ALINAMADI",
        [SignatureNotKept] = "IMZA KAYDEDILEMEDI",
        [InvoiceAlreadyKnown] = "GONDERILEN ZARF SISTEMDE DAHA ONCE KAYITLI OLAN BIR FATURAYI ICERMEKTEDIR.",
        [DocumentAlreadyKnown] = "GONDERILEN ZARF SISTEMDE DAHA ONCE KAYITLI OLAN BIR BELGEYI ICERMEKTEDIR.",
        [AuthorityNotChecked] = "YETKI KONTROL EDILEMEDI",
        [SenderUnitNotAuthorized] = "GONDERICI BIRIM YETKISI YOK",
        [PostBoxNotAuthorized] = "POSTA KUTUSU YETKISI YOK",
        [SignatureAuthorityNotChecked] = "IMZA YETKISI KONTROL EDILEMEDI",
        [SignatureOwnerNotAuthorized] = "IMZA SAHIBI YETKISIZ",
        [SignatureInvalid] = "GECERSIZ IMZA",
        [AddressNotChecked] = "ADRES KONTROL EDILEMEDI",
        [AddressNotFound] = "ADRES BULUNAMADI",
        [UserNotAdded] = "KULLANICI EKLENEMEDI",
        [UserNotRemoved] = "KULLANICI SILENEMEDI",
        [SystemResponseNotMade] = "SISTEM YANITI HAZIRLANAMADI",
        [SystemError] = "SISTEM HATASI",
        [Processed] = "ZARF BASARIYLA ISLENDI",
        [NotDeliveredRetrying] = "DOKUMAN BULUNAN ADRESE GONDERILEMEDI",
        [DeliveryFailed] = "DOKUMAN GONDERIMI BASARISIZ. TEKRAR GONDERME SONLANDI",
        [NoSystemResponse] = "HEDEFTEN SISTEM YANITI GELMEDI",
        [SystemResponseFailed] = "HEDEFTEN SISTEM YANITI BASARISIZ GELDI",
        [InvoiceCancelled] = "FATURA IPTALE KONU EDILDI",
        [Completed] = "BASARIYLA TAMAMLANDI",
        [HashesDiffer] = "OZET DEGERLER ESIT DEGIL",
        [EnvelopeAlreadyThere] = "ZARF ID SISTEMDE MEVCUT",
        [NotArchived] = "ZARF ARSIVE EKLENEMEDI",
        [NotQueued] = "ZARF KUYRUGA EKLENEMEDI",
        [EnvelopeNotFound] = "ZARF ID BULUNAMADI",
        [SystemFault] = "SISTEM HATASI",
        [InvalidEnvelopeName] = "GECERSIZ ZARF ADI",
        [SendingBlocked] = "PAKET GONDERMEYE VE SORGULAMAYA YETKINIZ GECICI OLARAK KALDIRILMISTIR.",
    };

    /// <summary>The text of a code; <c>TANIMSIZ</c> for a code the center does not know.</summary>
    public static string NameOf(int code) => s_names.TryGetValue(code, out string? name) ? name : "TANIMSIZ";

    /// <summary>Whether the code means the analysis refused the envelope (a code between 1100 and 1200): nothing is delivered.</summary>
    public static bool IsRefusal(int code) => code is > Processing and < Processed;

    /// <summary>Whether an envelope with this code still holds the invoices in it: a new envelope with one of them is refused (1163). An envelope that was refused, that could not be delivered (1215) or that the post box refused (1230) lets its invoices be sent again.</summary>
    public static bool HoldsItsDocuments(int code) => code is Queued or Processing or Processed or NotDeliveredRetrying or NoSystemResponse or Completed;
}
