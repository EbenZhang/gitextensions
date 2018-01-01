namespace ResourceManager.CommitDataRenders
{
    public interface IHeaderLabelFormatter
    {
        string FormatLabel(string label, int desiredLength, bool appendColon = true);
        string FormatLabelPlain(string label, int desiredLength);
    }
}