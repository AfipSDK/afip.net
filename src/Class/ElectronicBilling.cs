using System.Text.Json;

namespace Afip.Net;

public sealed class ElectronicBilling : AfipWebService
{
    protected override bool SoapV12 => true;
    protected override string WSDL => "wsfe-production.wsdl";
    protected override string URL => "https://servicios1.afip.gov.ar/wsfev1/service.asmx";
    protected override string WSDL_TEST => "wsfe.wsdl";
    protected override string URL_TEST => "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";

    public ElectronicBilling(Afip afip) : base(afip, new WebServiceConfig { Service = "wsfe" }) { }

    #region Sync

    public CreatePDFResponse CreatePDF(CreatePDFRequest data)
        => CreatePDFAsync(data).GetAwaiter().GetResult();

    public int GetLastVoucher(int salesPoint, int type)
        => GetLastVoucherAsync(salesPoint, type).GetAwaiter().GetResult();

    public Dictionary<string, object?> CreateVoucher(Dictionary<string, object?> data, bool returnResponse = false)
        => CreateVoucherAsync(data, returnResponse).GetAwaiter().GetResult();

    public Dictionary<string, object?> CreateNextVoucher(Dictionary<string, object?> data)
        => CreateNextVoucherAsync(data).GetAwaiter().GetResult();

    public Dictionary<string, object?>? GetVoucherInfo(int number, int salesPoint, int type)
        => GetVoucherInfoAsync(number, salesPoint, type).GetAwaiter().GetResult();

    public Dictionary<string, object?>? CreateCAEA(int period, int fortnight)
        => CreateCAEAAsync(period, fortnight).GetAwaiter().GetResult();

    public Dictionary<string, object?>? GetCAEA(int period, int fortnight)
        => GetCAEAAsync(period, fortnight).GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetSalesPoints()
        => GetSalesPointsAsync().GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetVoucherTypes()
        => GetVoucherTypesAsync().GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetConceptTypes()
        => GetConceptTypesAsync().GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetDocumentTypes()
        => GetDocumentTypesAsync().GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetAliquotTypes()
        => GetAliquotTypesAsync().GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetCurrenciesTypes()
        => GetCurrenciesTypesAsync().GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetOptionsTypes()
        => GetOptionsTypesAsync().GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetTaxTypes()
        => GetTaxTypesAsync().GetAwaiter().GetResult();

    public Dictionary<string, object?> GetServerStatus()
        => GetServerStatusAsync().GetAwaiter().GetResult();

    #endregion

    #region Async

    public async Task<CreatePDFResponse> CreatePDFAsync(CreatePDFRequest data)
    {
        var body = new Dictionary<string, object?>
        {
            ["file_name"] = data.FileName,
            ["template"] = data.Template
        };

        if (!string.IsNullOrEmpty(data.SendTo))
            body["send_to"] = data.SendTo;

        return await afip.AdminClient.MakeRequestAsync<CreatePDFResponse>(HttpMethod.Post, "v1/pdfs", body);
    }

    public async Task<int> GetLastVoucherAsync(int salesPoint, int type)
    {
        var result = await ExecuteAfipAsync("FECompUltimoAutorizado", new Dictionary<string, object?>
        {
            ["PtoVta"] = salesPoint,
            ["CbteTipo"] = type
        });

        return result["CbteNro"] is JsonElement el ? el.GetInt32() : 0;
    }

    public async Task<Dictionary<string, object?>> CreateVoucherAsync(Dictionary<string, object?> data, bool returnResponse = false)
    {
        data = new Dictionary<string, object?>(data);

        var cantReg = Convert.ToInt32(data["CbteHasta"]) - Convert.ToInt32(data["CbteDesde"]) + 1;
        var ptoVta = Convert.ToInt32(data["PtoVta"]);
        var cbteTipo = Convert.ToInt32(data["CbteTipo"]);

        data.Remove("CantReg");
        data.Remove("PtoVta");
        data.Remove("CbteTipo");

        if (data.ContainsKey("Tributos")) data["Tributos"] = new Dictionary<string, object?> { ["Tributo"] = data["Tributos"] };
        if (data.ContainsKey("Iva")) data["Iva"] = new Dictionary<string, object?> { ["AlicIva"] = data["Iva"] };
        if (data.ContainsKey("CbtesAsoc")) data["CbtesAsoc"] = new Dictionary<string, object?> { ["CbteAsoc"] = data["CbtesAsoc"] };
        if (data.ContainsKey("Compradores")) data["Compradores"] = new Dictionary<string, object?> { ["Comprador"] = data["Compradores"] };
        if (data.ContainsKey("Opcionales")) data["Opcionales"] = new Dictionary<string, object?> { ["Opcional"] = data["Opcionales"] };

        var req = new Dictionary<string, object?>
        {
            ["FeCAEReq"] = new Dictionary<string, object?>
            {
                ["FeCabReq"] = new Dictionary<string, object?>
                {
                    ["CantReg"] = cantReg,
                    ["PtoVta"] = ptoVta,
                    ["CbteTipo"] = cbteTipo
                },
                ["FeDetReq"] = new Dictionary<string, object?>
                {
                    ["FECAEDetRequest"] = data
                }
            }
        };

        var result = await ExecuteAfipAsync("FECAESolicitar", req);

        if (returnResponse)
            return result;

        var detResp = ((JsonElement)result["FeDetResp"]!).GetProperty("FECAEDetResponse");
        if (detResp.ValueKind == JsonValueKind.Array)
            detResp = detResp[0];

        return new Dictionary<string, object?>
        {
            ["CAE"] = detResp.GetProperty("CAE").GetString(),
            ["CAEFchVto"] = FormatDate(detResp.GetProperty("CAEFchVto").GetString()!)
        };
    }

    public async Task<Dictionary<string, object?>> CreateNextVoucherAsync(Dictionary<string, object?> data)
    {
        var ptoVta = Convert.ToInt32(data["PtoVta"]);
        var cbteTipo = Convert.ToInt32(data["CbteTipo"]);

        var lastVoucher = await GetLastVoucherAsync(ptoVta, cbteTipo);
        var voucherNumber = lastVoucher + 1;

        data = new Dictionary<string, object?>(data)
        {
            ["CbteDesde"] = voucherNumber,
            ["CbteHasta"] = voucherNumber
        };

        var res = await CreateVoucherAsync(data);
        res["voucherNumber"] = voucherNumber;
        return res;
    }

    public async Task<Dictionary<string, object?>?> GetVoucherInfoAsync(int number, int salesPoint, int type)
    {
        try
        {
            var result = await ExecuteAfipAsync("FECompConsultar", new Dictionary<string, object?>
            {
                ["FeCompConsReq"] = new Dictionary<string, object?>
                {
                    ["CbteNro"] = number,
                    ["PtoVta"] = salesPoint,
                    ["CbteTipo"] = type
                }
            });

            return ExtractObject(result, "ResultGet");
        }
        catch (AfipWebServiceException ex) when (ex.Code == 602)
        {
            return null;
        }
    }

    public async Task<Dictionary<string, object?>?> CreateCAEAAsync(int period, int fortnight)
    {
        var result = await ExecuteAfipAsync("FECAEASolicitar", new Dictionary<string, object?>
        {
            ["Periodo"] = period,
            ["Orden"] = fortnight
        });

        return ExtractObject(result, "ResultGet");
    }

    public async Task<Dictionary<string, object?>?> GetCAEAAsync(int period, int fortnight)
    {
        var result = await ExecuteAfipAsync("FECAEAConsultar", new Dictionary<string, object?>
        {
            ["Periodo"] = period,
            ["Orden"] = fortnight
        });

        return ExtractObject(result, "ResultGet");
    }

    public async Task<List<Dictionary<string, object?>>?> GetSalesPointsAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetPtosVenta"), "ResultGet", "PtoVenta");

    public async Task<List<Dictionary<string, object?>>?> GetVoucherTypesAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetTiposCbte"), "ResultGet", "CbteTipo");

    public async Task<List<Dictionary<string, object?>>?> GetConceptTypesAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetTiposConcepto"), "ResultGet", "ConceptoTipo");

    public async Task<List<Dictionary<string, object?>>?> GetDocumentTypesAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetTiposDoc"), "ResultGet", "DocTipo");

    public async Task<List<Dictionary<string, object?>>?> GetAliquotTypesAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetTiposIva"), "ResultGet", "IvaTipo");

    public async Task<List<Dictionary<string, object?>>?> GetCurrenciesTypesAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetTiposMonedas"), "ResultGet", "Moneda");

    public async Task<List<Dictionary<string, object?>>?> GetOptionsTypesAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetTiposOpcional"), "ResultGet", "OpcionalTipo");

    public async Task<List<Dictionary<string, object?>>?> GetTaxTypesAsync()
        => ExtractList(await ExecuteAfipAsync("FEParamGetTiposTributos"), "ResultGet", "TributoTipo");

    public async Task<Dictionary<string, object?>> GetServerStatusAsync()
        => await ExecuteAfipAsync("FEDummy");

    #endregion


    private async Task<Dictionary<string, object?>> ExecuteAfipAsync(string operation, Dictionary<string, object?>? parameters = null)
    {
        parameters ??= [];
        foreach (var kv in await GetWSInitialRequest(operation))
            parameters[kv.Key] = kv.Value;

        var results = await ExecuteRequestAsync<Dictionary<string, object?>>(operation, parameters);

        CheckErrors(operation, results);

        return ExtractObject(results, operation + "Result") ?? [];
    }

    private async Task<Dictionary<string, object?>> GetWSInitialRequest(string operation)
    {
        if (string.Equals(operation, "FEDummy", StringComparison.OrdinalIgnoreCase))
            return [];

        var ta = await afip.GetServiceTaAsync("wsfe");
        return new Dictionary<string, object?>
        {
            ["Auth"] = new Dictionary<string, object?>
            {
                ["Token"] = ta.Token,
                ["Sign"] = ta.Sign,
                ["Cuit"] = afip.Options.CUIT
            }
        };
    }

    private static void CheckErrors(string operation, Dictionary<string, object?> results)
    {
        var key = operation + "Result";
        if (!results.TryGetValue(key, out var resObj) || resObj is not JsonElement res) return;

        if (operation == "FECAESolicitar" && res.TryGetProperty("FeDetResp", out var feDetResp))
        {
            var detResponse = feDetResp.GetProperty("FECAEDetResponse");
            if (detResponse.ValueKind == JsonValueKind.Array)
            {
                if (detResponse.GetArrayLength() > 1) return;
                detResponse = detResponse[0];
            }

            if (detResponse.TryGetProperty("Observaciones", out var obs) &&
                detResponse.TryGetProperty("Resultado", out var resultado) &&
                resultado.GetString() != "A" &&
                obs.TryGetProperty("Obs", out var obsErr))
            {
                ThrowError(obsErr);
                return;
            }
        }

        if (res.TryGetProperty("Errors", out var errors) && errors.TryGetProperty("Err", out var err))
            ThrowError(err);
    }

    private static void ThrowError(JsonElement err)
    {
        var errItem = err.ValueKind == JsonValueKind.Array ? err[0] : err;
        var code = errItem.GetProperty("Code").GetInt32();
        var msg = errItem.GetProperty("Msg").GetString() ?? string.Empty;
        throw new AfipWebServiceException($"({code}) {msg}", code);
    }

    private static Dictionary<string, object?>? ExtractObject(Dictionary<string, object?> result, string key)
    {
        return result.TryGetValue(key, out var val) && val is JsonElement el
            ? JsonSerializer.Deserialize<Dictionary<string, object?>>(el.GetRawText())
            : null;
    }

    private static List<Dictionary<string, object?>>? ExtractList(Dictionary<string, object?> result, string key1, string key2)
    {
        if (!result.TryGetValue(key1, out var val1) || val1 is not JsonElement el1) return null;
        if (!el1.TryGetProperty(key2, out var el2)) return null;
        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(el2.GetRawText());
    }

    private static string FormatDate(string date)
    {
        if (date.Length == 8)
            return $"{date.Substring(0, 4)}-{date.Substring(4, 2)}-{date.Substring(6, 2)}";
        return date;
    }
}
