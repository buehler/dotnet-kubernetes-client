﻿using System.Collections.Generic;

namespace DotnetKubernetesClient.LabelSelectors;

/// <summary>
/// Label-selector that checks if a certain label does not contain
/// a specific value (out of a list of values).
/// Note that "label notin (value)" is the same as "label != value".
/// </summary>
public record NotEqualsSelector : ILabelSelector
{
    public NotEqualsSelector(string label, params string[] values) => (Label, Values) = (label, values);

    public string Label { get; set; }

    public IEnumerable<string> Values { get; set; }

    public string ToExpression() => $"{Label} notin ({string.Join(",", Values)})";
}
