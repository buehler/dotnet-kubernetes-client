namespace DotnetKubernetesClient.LabelSelectors
{
    /// <summary>
    /// Selector that checks if a certain label exists.
    /// </summary>
    public record ExistsSelector : ILabelSelector
    {
        public ExistsSelector(string label) => Label = label;

        public string Label { get; }

        public string ToExpression() => $"{Label}";
    }
}
