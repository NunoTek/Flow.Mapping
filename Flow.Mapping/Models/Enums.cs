namespace Flow.Mapping.Models;

public enum MapFromTypes
{
    XPath = 1,
    Value
}

// TODO deplacer dans chaque projet
public enum ValueResolverTypes
{
    None = 0,
    StringBool,
    DistinctByValue,
    StringDateFormat,

    FrenchNumberFormat,
    FrenchCurrencyFormat,
    LoyerAnnuelToMensuel,

    ExtractFileNameFromUri,
    UnescapeHtml,
    MapIfDecimalValueGreaterThanZero,

    AverageValue,
    NumberToListOfMappedElement,
    LotResolver,
    MapLotSecondaireOrAnnexeLot,
    ProgrammeMultiFiscalite,

    LpPromotionLotSecondaires,
    AcapaceAnnexes,
    SiglaNeufFiscaliteObjectif,
    MultiOrientation,
    PichetDoubleOrientation,
    FinalPriceResolver,

    AddTVAResolver,
    TarifLotResolver,
    ConfianceLotSecondaires,
    
    FidexiLotResolver,
    DiviserPrixLotSecondaire,

    HtmlStyleSanitizer,

    EiffageContentResolver,    
}