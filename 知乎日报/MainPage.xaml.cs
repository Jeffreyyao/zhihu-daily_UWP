using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using System.Net;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Data.Json;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace 知乎日报
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public string url { get; set; }
		public string zhihuPath = "http://news-at.zhihu.com/api/3/news/";
		DateTime date = DateTime.Today.AddDays(1);
		System.Collections.ObjectModel.ObservableCollection<zhihuItemSource> zhihuRecentCollection = new System.Collections.ObjectModel.ObservableCollection<zhihuItemSource>();

		public MainPage()
		{
			this.InitializeComponent();
			setStatusBar();
			System.Diagnostics.Debug.WriteLine(date);
			NavigationCacheMode = NavigationCacheMode.Required;
			hamburger.Click += new RoutedEventHandler((object a, RoutedEventArgs b) =>
				{
					splitview.IsPaneOpen = true;
				});
			collection.Tapped += new TappedEventHandler((object a, TappedRoutedEventArgs b) =>
				{
					title.Text = " 我的收藏"; splitview.IsPaneOpen = false; generateCollectionView();
				});
			latest.Tapped += new TappedEventHandler((object a, TappedRoutedEventArgs b) =>
				{
					title.Text = " 知乎日报"; splitview.IsPaneOpen = false;
				});
			settings.Tapped += new TappedEventHandler((object a, TappedRoutedEventArgs b) =>
				{
					title.Text = " 设置"; splitview.IsPaneOpen = false;
				});
			load_latest_listview();
			generateCollectionView();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			generateCollectionView();
		}

		void setStatusBar()
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
			{
				var statusbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
				statusbar.BackgroundOpacity = 1;
				statusbar.BackgroundColor = Windows.UI.Color.FromArgb((byte)0xFF, (byte)0x44, (byte)0x88, (byte)0xff);
				statusbar.ForegroundColor = Windows.UI.Colors.White;
			}
		}

		async Task<List<object>> getRecentStringList(string st)
		{
			List<object> finalResult = new List<object>();
			List<Uri> imageUriResult = new List<Uri>();
			List<string> titleResult = new List<string>();
			List<int> idResult = new List<int>();
			HttpWebRequest request = WebRequest.CreateHttp(zhihuPath + st);
			WebResponse response = await request.GetResponseAsync();
			Stream stream = response.GetResponseStream();
			StreamReader reader = new StreamReader(stream);
			string str = await reader.ReadToEndAsync();
			JsonObject jo = JsonValue.Parse(str).GetObject();
			var jsonStories = jo.GetNamedArray("stories");
			foreach (IJsonValue ijv in jsonStories)
			{
				imageUriResult.Add(new Uri(ijv.GetObject().GetNamedArray("images").GetStringAt(0)));
				titleResult.Add(ijv.GetObject().GetNamedString("title"));
				idResult.Add((int)ijv.GetObject().GetNamedNumber("id"));
			}
			finalResult.Add(imageUriResult); finalResult.Add(titleResult); finalResult.Add(idResult);
			progressring.IsActive = false;
			return finalResult;
		}

		async void generateCollectionView()
		{
			StorageFolder folder = ApplicationData.Current.LocalFolder;
			var file = await folder.CreateFileAsync("collect.txt", CreationCollisionOption.OpenIfExists);
			collectionListview.Children.Clear();
			collectionListview.RowDefinitions.Clear();
			foreach (string str in await FileIO.ReadLinesAsync(file))
			{
				if (!(str == ""))
				{
					string url = str.Split(new string[] { "," }, StringSplitOptions.None)[0];
					string title = str.Split(new string[] { "," }, StringSplitOptions.None)[1];
					collectionListview.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(48) });
					Button btn = new Button() { Content = title };
					btn.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) =>
					{
						Frame.Navigate(typeof(ContentPage), new zhihuItemSource() { url = url, title = title });
					});
					collectionListview.Children.Add(btn);
					Grid.SetRow(btn, collectionListview.RowDefinitions.Count - 1);
				}
			}
		}

		async void load_latest_listview()
		{
			List<object> objectContainer = await getRecentStringList("latest");
			List<Uri> imageUriResult = (List<Uri>)objectContainer[0];
			List<string> titleResult = (List<string>)objectContainer[1];
			List<int> idResult = (List<int>)objectContainer[2];
			for (int i = 0; i < imageUriResult.Count; i++)
			{
				zhihuRecentCollection.Add(new zhihuItemSource(imageUriResult[i], titleResult[i], idResult[i]));
			}
		}

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			zhihuItemSource source = (zhihuItemSource)e.ClickedItem;
			url = source.url;
			Uri uri = source.img;
			var parameter = new zhihuItemSource() { url = url, img = uri };
			Frame.Navigate(typeof(知乎日报.ContentPage), parameter);
			System.Diagnostics.Debug.WriteLine(typeof(ContentPage));
			System.Diagnostics.Debug.WriteLine(typeof(MainPage));
		}

		private void latestListview_ItemClick(object sender, ItemClickEventArgs e)
		{
			zhihuItemSource source = (zhihuItemSource)e.ClickedItem;
			url = source.url;
			Uri uri = source.img;
			var parameter = new zhihuItemSource() { url = url, img = uri, title = source.title };
			Frame.Navigate(typeof(知乎日报.ContentPage), parameter);
		}

		private void AppBarButton_Click(object sender, RoutedEventArgs e)
		{
			zhihuRecentCollection.Clear();
			load_latest_listview();
			generateCollectionView();
			dateTextBlock.Text = "";
		}

		private async void back_Click(object sender, RoutedEventArgs e)
		{
			date = date.AddDays(1);
			if (date > DateTime.Today) back.IsEnabled = false;
			dateTextBlock.Text = date.AddDays(-1).ToString("yyyy-MM-dd");
			List<object> objectContainer = await getRecentStringList("before/" + date.ToString("yyyyMMdd"));
			List<Uri> imageUriResult = (List<Uri>)objectContainer[0];
			List<string> titleResult = (List<string>)objectContainer[1];
			List<int> idResult = (List<int>)objectContainer[2];
			zhihuRecentCollection.Clear();
			for (int i = 0; i < imageUriResult.Count; i++)
			{
				zhihuRecentCollection.Add(new zhihuItemSource(imageUriResult[i], titleResult[i], idResult[i]));
			}
		}

		private async void forward_Click(object sender, RoutedEventArgs e)
		{
			date = date.AddDays(-1);
			back.IsEnabled = true;
			dateTextBlock.Text = date.AddDays(-1).ToString("yyyy-MM-dd");
			List<object> objectContainer = await getRecentStringList("before/" + date.ToString("yyyyMMdd"));
			List<Uri> imageUriResult = (List<Uri>)objectContainer[0];
			List<string> titleResult = (List<string>)objectContainer[1];
			List<int> idResult = (List<int>)objectContainer[2];
			zhihuRecentCollection.Clear();
			for (int i = 0; i < imageUriResult.Count; i++)
			{
				zhihuRecentCollection.Add(new zhihuItemSource(imageUriResult[i], titleResult[i], idResult[i]));
			}
		}
	}
}

public class zhihuItemSource
{
	public Uri img { get; set; }
	public string title { get; set; }
	public string url { get; set; }

	public zhihuItemSource(Uri imguri, string strtitle, int idInt)
	{
		img = imguri;
		title = strtitle;
		url = "http://news-at.zhihu.com/api/3/news/" + idInt.ToString();
	}

	public zhihuItemSource() { }
}
