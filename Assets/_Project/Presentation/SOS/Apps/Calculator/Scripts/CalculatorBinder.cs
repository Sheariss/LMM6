using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.SOS.Calculator
{
    public sealed class CalculatorBinder : UIBinder
    {
        public VisualElement Root { get; }

        public Label ExpressionLabel { get; }
        public TextField DisplayField { get; }


        public Button PercentButton { get; }
        public Button ClearEntryButton { get; }
        public Button ClearButton { get; }
        public Button BackspaceButton { get; }

        public Button OneOverButton { get; }
        public Button SquareButton { get; }
        public Button RootButton { get; }
        public Button DivideButton { get; }

        public Button SevenButton { get; }
        public Button EightButton { get; }
        public Button NineButton { get; }
        public Button MultiplyButton { get; }

        public Button FourButton { get; }
        public Button FiveButton { get; }
        public Button SixButton { get; }
        public Button SubtractButton { get; }

        public Button OneButton { get; }
        public Button TwoButton { get; }
        public Button ThreeButton { get; }
        public Button AddButton { get; }

        public Button SignButton { get; }
        public Button ZeroButton { get; }
        public Button DecimalButton { get; }
        public Button EqualButton { get; }

        public CalculatorBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[CalculatorBinder] Root VisualElement is null.");
                return;
            }

            Root = Bind<VisualElement>(root, "CalculatorRoot");

            ExpressionLabel = Bind<Label>(root, "CalculatorExpression");
            DisplayField = Bind<TextField>(root, "CalculatorDisplay");

            PercentButton = Bind<Button>(root, "PercentButton");
            ClearEntryButton = Bind<Button>(root, "ClearEntryButton");
            ClearButton = Bind<Button>(root, "ClearButton");
            BackspaceButton = Bind<Button>(root, "BackspaceButton");


            OneOverButton = Bind<Button>(root, "FractionButton");
            SquareButton = Bind<Button>(root, "SquareButton");
            RootButton = Bind<Button>(root, "RootButton");
            DivideButton = Bind<Button>(root, "DivideButton");

            SevenButton = Bind<Button>(root, "SevenButton");
            EightButton = Bind<Button>(root, "EightButton");
            NineButton = Bind<Button>(root, "NineButton");
            MultiplyButton = Bind<Button>(root, "MultiplyButton");

            FourButton = Bind<Button>(root, "FourButton");
            FiveButton = Bind<Button>(root, "FiveButton");
            SixButton = Bind<Button>(root, "SixButton");
            SubtractButton = Bind<Button>(root, "SubtractButton");

            OneButton = Bind<Button>(root, "OneButton");
            TwoButton = Bind<Button>(root, "TwoButton");
            ThreeButton = Bind<Button>(root, "ThreeButton");
            AddButton = Bind<Button>(root, "AddButton");


            SignButton = Bind<Button>(root, "SignButton");
            ZeroButton = Bind<Button>(root, "ZeroButton");
            DecimalButton = Bind<Button>(root, "DecimalButton");
            EqualButton = Bind<Button>(root, "EqualButton");
        }
    }
}