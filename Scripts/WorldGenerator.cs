using Godot;
using RuntimeDataSpace;
using System;

public partial class WorldGenerator : Node2D
{
	[Export] MapRegionResource[] regions;
	[Export] AudioStreamPlayer SoundPlayer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
