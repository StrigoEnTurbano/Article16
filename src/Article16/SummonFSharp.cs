using Godot;

[GlobalClass]
[Icon(@"res://FSharpLogo.png")]
public partial class SummonFSharp : Node
{
	public override void _Ready() => Article16.Core.EntryPoint.summonFSharp(this);
}
