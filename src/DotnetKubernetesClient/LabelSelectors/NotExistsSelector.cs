namespace DotnetKubernetesClient.LabelSelectors
{
    /// <summary>
    /// Selector that checks if a certain label does not exist.
    /// </summary>
    public record NotExistsSelector : ILabelSelector
    {
        public NotExistsSelector(string label) => Label = label;

        public string Label { get; }

        public string ToExpression() => $"!{Label}";
    }
}
