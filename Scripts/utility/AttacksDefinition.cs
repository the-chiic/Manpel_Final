using Godot;
using System;

namespace Attacks
{
	public enum EnemyAttacksType : byte
	{
		None = 0,
		Fire,	
		Water,
		Seed,
		Explosion
	}

	public enum PlayerAttacksType : byte
	{
		None = 0,
		Seed,
		Fire,
		Water,
		Flesh
	}
}
