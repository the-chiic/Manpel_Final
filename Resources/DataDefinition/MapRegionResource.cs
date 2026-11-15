using Godot;
using System;

[GlobalClass]
public partial class MapRegionResource : Resource
{
    [Export] public PackedScene Scene;
    [Export] public int Id;
}
