﻿using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Dots.Services;
using Microsoft.Extensions.Logging;

namespace Dots;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		
#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton<DotnetService>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<MainViewModel>();
		return builder.Build();
	}
}
