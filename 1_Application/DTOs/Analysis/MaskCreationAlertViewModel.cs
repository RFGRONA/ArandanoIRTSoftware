namespace ArandanoIRT.Web._1_Application.DTOs.Analysis;

public class MaskCreationAlertViewModel
{
    public string UserName { get; set; }
    public List<string> PlantNames { get; set; }
    public string CtaButtonUrl { get; set; } = "/Admin/Plants/Index";
}