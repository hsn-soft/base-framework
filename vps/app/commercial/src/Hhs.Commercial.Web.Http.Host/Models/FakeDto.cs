using HsnSoft.Base.Application.Dtos;

namespace Hhs.Commercial.Web.Models;

public class FakeDto : EntityDto<Guid>
{
    public DateTime FakeDate { get; set; }

    public string FakeCode { get; set; }

    public FakeState FakeState { get; set; }
}