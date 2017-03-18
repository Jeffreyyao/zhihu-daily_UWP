using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Json;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace 知乎日报
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ContentPage : Page
	{
		string url;
		string title;

		public ContentPage()
		{
			InitializeComponent();
			setBackButton();
			setStatusBar();
		}

		void setStatusBar()
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
			{
				var statusbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
				statusbar.BackgroundOpacity = 1;
				statusbar.BackgroundColor = Windows.UI.Color.FromArgb((byte)0xFf, (byte)0x44, (byte)0x88, (byte)0xff);
				statusbar.ForegroundColor = Windows.UI.Colors.White;
			}
		}

		void setBackButton()
		{
			var currentView = SystemNavigationManager.GetForCurrentView();
			currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
			
			currentView.BackRequested += new EventHandler<BackRequestedEventArgs>((object sender, BackRequestedEventArgs e) => 
			{
				e.Handled = true;
				Frame.Navigate(typeof(MainPage));
			});
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var parameter = (zhihuItemSource)e.Parameter;
			url = parameter.url;
			title = parameter.title;

			if (await checkIfCollected()) addcollection.Foreground = new SolidColorBrush(Windows.UI.Colors.Orange);
			addcollection.Click += new RoutedEventHandler(async(object sender, RoutedEventArgs a) => {
				if (await checkIfCollected()) { disCollect(); }
				else { collect(); }
			});

			WebRequest request = HttpWebRequest.Create(url);
			WebResponse response = await request.GetResponseAsync();
			Stream stream = response.GetResponseStream();
			StreamReader reader = new StreamReader(stream);
			string json = await reader.ReadToEndAsync();
			JsonValue jv = JsonValue.Parse(json);
			string body = jv.GetObject().GetNamedString("body");
			string img = jv.GetObject().GetNamedString("image");

			string css = jv.GetObject().GetNamedArray("css").GetStringAt(0);
			request = HttpWebRequest.Create(css);
			response = await request.GetResponseAsync();
			stream = response.GetResponseStream();
			reader = new StreamReader(stream);
			css = reader.ReadToEnd();
			var html = body + "<style>" + css + "</style>" + "<script>document.body.style.zoom=2.5</script>";
			html = html.Replace("<div class=\"img-place-holder\">", "<div><img class=\"content-image\" width=\"100%\" src=\""+img+"\" alt=\"\"/>");
			System.Diagnostics.Debug.WriteLine(html);
			webview.NavigateToString(html);
		}

		private void webview_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			progressring.IsActive = false;
		}

		async Task<bool> checkIfCollected()
		{
			StorageFolder folder = ApplicationData.Current.LocalFolder;
			var file = await folder.CreateFileAsync("collect.txt", CreationCollisionOption.OpenIfExists);
			string content = await FileIO.ReadTextAsync(file);
			return content.Contains(url + "," + title);
		}

		async void collect()
		{
			StorageFolder folder = ApplicationData.Current.LocalFolder;
			var file = await folder.CreateFileAsync("collect.txt", CreationCollisionOption.OpenIfExists);
			string content = await FileIO.ReadTextAsync(file);
			if (!content.Contains(url + "," + title))
			{
				await FileIO.AppendLinesAsync(file, new string[] { url + "," + title });
			}
			addcollection.Foreground = new SolidColorBrush(Windows.UI.Colors.Orange);
		}

		async void disCollect()
		{
			StorageFolder folder = ApplicationData.Current.LocalFolder;
			var file = await folder.CreateFileAsync("collect.txt", CreationCollisionOption.OpenIfExists);
			string content = await FileIO.ReadTextAsync(file);
			content = content.Replace(url + "," + title, "");
			await FileIO.WriteTextAsync(file, content);
			addcollection.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
		}

		private void hamburger_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(MainPage));
		}
	}
}
