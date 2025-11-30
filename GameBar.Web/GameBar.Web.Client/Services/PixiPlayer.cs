using System;

namespace GameBar.Web.Client.Services;

/// <summary>
/// Lightweight DTO passed to the Pixi JS layer for rendering.
/// </summary>
public readonly record struct PixiPlayer(string Id, float X, float Y, int FrameIndex, string Anim, int FrameWidth, int FrameHeight);
