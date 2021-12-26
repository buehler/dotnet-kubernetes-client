using System;
using System.Linq;
using DotnetKubernetesClient;
using k8s.Models;

var client = new KubernetesClient();
var version = await client.GetServerVersion();
Console.WriteLine(
    $"Version: {version.Major}.{version.Minor}; built at {version.BuildDate}; git commit {version.GitCommit}.");

var singleNs = await client.Get<V1Namespace>("default");
Console.WriteLine($"Found default namespace {singleNs.Uid()}");

var ns = await client.List<V1Namespace>();
Console.WriteLine($"Found {ns.Count} namespaces;\n{string.Join("\n", ns.Select(n => n.Metadata?.Name))}");
