using Xtb.XApi.Records;

namespace Xtb.XApi.Responses;

public sealed class CurrentUserDataResponse : BaseResponse
{
    public CurrentUserDataResponse()
        : base()
    { }

    public CurrentUserDataResponse(string body)
        : base(body)
    {
        if (ReturnData is null)
            return;

        var ob = ReturnData.AsObject();
        CurrentUserDataRecord = new();
        CurrentUserDataRecord.FieldsFromJsonObject(ob);
    }

    public CurrentUserDataRecord? CurrentUserDataRecord { get; init; }
}