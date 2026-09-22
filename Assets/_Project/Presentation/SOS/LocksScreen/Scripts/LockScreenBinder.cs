using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.SOS.LockScreen
{
    public class LockScreenBinder : UIBinder
    {
        // -------------------- UI ELEMENTS --------------------
        public VisualElement Root { get; }

        public VisualElement LockBackgroundImage { get; }
        public VisualElement Content { get; }

        public Label TimeLabel { get; }
        public Label DateLabel { get; }

        // Weather widget
        public Button WeatherWidgetBtn { get; }
        public Image WeatherIconImage { get; }
        public Label WeatherTempLabel { get; }
        public Label DescriptionLabel { get; }
        public Label DescTempLabel { get; }

        // Market Widget
        public Button MarketsWidgetBtn { get; }
        public GroupBox Stock1Group { get; }
        public Label  Stock1Name { get; }
        public Label Stock1PercentageChange { get; }
        public Label Stock1Price { get; }

        public GroupBox Stock2Group { get; }
        public Label Stock2Name { get; }
        public Label Stock2PercentageChange { get; }
        public Label Stock2Price { get; }

        public GroupBox Stock3Group { get; }
        public Label Stock3Name { get; }
        public Label Stock3PercentageChange { get; }
        public Label Stock3Price { get; }

        public Button TrafficWidgetBtn { get; }
        public Image TrafficMap { get; }
        public Label TrafficDescription { get; }

        public Button NewsWidgetBtn { get; }
        public Image NewsImage { get; }
        public Label NewsHeadline { get; }
        public Label NewsSubtitle { get; }

        public Button LockWifiButton { get; }
        public Button LockAccessibilityButton { get; }
        public Button LockBatteryButton { get; }

        // -------------------- CONSTRUCTOR --------------------
        public LockScreenBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[LockScreenBinder] Root VisualElement is null.");
                return;
            }


            Root = Bind<VisualElement>(root, "LockScreenRoot");

            LockBackgroundImage = Bind<VisualElement>(root, "LSR-BackgroundImage");

            Content = Bind<VisualElement>(root, "LockScreenContent");

            TimeLabel = Bind<Label>(Content, "TimeLabel");
            DateLabel = Bind<Label>(Content, "DateLabel");

            // Weather widget
            WeatherWidgetBtn = Bind<Button>(Content, "WeatherWidgetBtn");

            WeatherIconImage = Bind<Image>(WeatherWidgetBtn, "WeatherIconImage");
            WeatherTempLabel = Bind<Label>(WeatherWidgetBtn, "WeatherTempLabel");
            DescriptionLabel = Bind<Label>(WeatherWidgetBtn, "DescriptionLabel");
            DescTempLabel = Bind<Label>(WeatherWidgetBtn, "DescTempLabel");

            // Market Widget
            MarketsWidgetBtn = Bind<Button>(Content, "MarketsWidgetBtn");
            Stock1Group = Bind<GroupBox>(MarketsWidgetBtn, "Stock1Group");
            Stock1Name = Bind<Label>(Stock1Group, "StockName");
            Stock1PercentageChange = Bind<Label>(Stock1Group, "StockPercentChange");
            Stock1Price = Bind<Label>(Stock1Group, "StockPrice");

            Stock2Group = Bind<GroupBox>(MarketsWidgetBtn, "Stock2Group");
            Stock2Name = Bind<Label>(Stock2Group, "StockName");
            Stock2PercentageChange = Bind<Label>(Stock2Group, "StockPercentChange");
            Stock2Price = Bind<Label>(Stock2Group, "StockPrice");

            Stock3Group = Bind<GroupBox>(MarketsWidgetBtn, "Stock3Group");
            Stock3Name = Bind<Label>(Stock3Group, "StockName");
            Stock3PercentageChange = Bind<Label>(Stock3Group, "StockPercentChange");
            Stock3Price = Bind<Label>(Stock3Group, "StockPrice");

            // Traffic Widget
            TrafficWidgetBtn = Bind<Button>(Content, "TrafficWidgetBtn");
            TrafficMap = Bind<Image>(TrafficWidgetBtn, "TrafficMap");
            TrafficDescription = Bind<Label>(TrafficWidgetBtn, "TrafficDescription");

            // News widget
            NewsWidgetBtn = Bind<Button>(Content, "NewsWidgetBtn");
            NewsImage = Bind<Image>(NewsWidgetBtn, "NewsImage");
            NewsHeadline = Bind<Label>(NewsWidgetBtn, "NewsHeadline");
            NewsSubtitle = Bind<Label>(NewsWidgetBtn, "NewsSubtitle");

            // Status buttons
            LockWifiButton = Bind<Button>(Content, "LockWifiButton");
            LockAccessibilityButton = Bind<Button>(Content, "LockAccessibilityButton");
            LockBatteryButton = Bind<Button>(Content, "LockBatteryButton");

        }
    }
}