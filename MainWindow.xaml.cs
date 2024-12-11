using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MathEvaluation.Context;
using MathEvaluation.Extensions;
using MathEvaluation.Parameters;

namespace lab2
{
    public partial class MainWindow : Window
    {
        private List<TextBox> listOperands = [];
        public MainWindow()
        {
            InitializeComponent();
            BaseLabel.Content = "Операторы: ()\t|(+)\t&(*)\t->\t<->\txor\t!(оператор не)";
        }

        private string Validate(string str)
        {
            return str.Replace(" ", "")
                .Replace("xor", "⊕")
                .Replace("!", "¬")
                .Replace("<->", "==")
                .Replace("->", "⇒")
                .Replace("+", "|")
                .Replace("*", "&")
                .Replace("|", "∨")
                .Replace("&", "∧");
        }

        private void textbox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                string expression = Validate(Content.Text.ToString());
                Simplify(expression);
                LoadOperands(GetVariables(expression).ToList());
                ValidateContent.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                ValidateContent.Visibility = Visibility.Visible;
            }
        }

        private HashSet<string> GetVariables(string expression)
        {
            var variables = new HashSet<string>();
            var matches = Regex.Matches(expression, @"\b[A-Za-z]+\b");
            foreach (Match match in matches)
            {
                variables.Add(match.Value);
            }
            return variables;
        }

        private bool EvaluateBooleanExpression(string expression, List<string> variables, bool[] values)
        {
            var parameters = new MathParameters();
            for (int i = 0; i < variables.Count; i++)
            {
                parameters.BindVariable(values[i], variables[i]);
            }
            var result = expression.EvaluateBoolean(parameters, new ScientificMathContext());

            return result;
        }

        private string ToSDNF(bool[,] truthTable, List<string> variables)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < truthTable.GetLength(0); i++)
            {
                if(truthTable[i, truthTable.GetLength(1)-1] == true)
                {
                    if (sb.Length != 0)
                        sb.Append("∨");
                    for(int j = truthTable.GetLength(1)-2; j >= 0; j--)
                    {
                        sb.Append((truthTable[i, j] ? "" : "¬") + variables[j] + "∧");
                    }
                    if(sb.Length != 0)
                        sb.Remove(sb.Length - 1, 1);
                }
            }
            return sb.ToString();
        }

        private string ToSKNF(bool[,] truthTable, List<string> variables)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < truthTable.GetLength(0); i++)
            {
                if (truthTable[i, truthTable.GetLength(1) - 1] == false)
                {
                    if (sb.Length != 0)
                        sb.Append("∧");
                    sb.Append("(");
                    for (int j = truthTable.GetLength(1) - 2; j >= 0; j--)
                    {
                        sb.Append((truthTable[i, j] ? "¬" : "") + variables[j] + "∨");
                    }
                    if (sb.Length != 0)
                        sb.Remove(sb.Length - 1, 1);
                    sb.Append(")");
                }
            }
            return sb.ToString();
        }

        public void Simplify(string expression)
        {
            var variables = GetVariables(expression).Reverse().ToList();
            bool[] values = new bool[variables.Count];
            bool[,] truthTable = new bool[(int)Math.Pow(2, values.Length), variables.Count + 1];
            for (int i = 0; i < Math.Pow(2, values.Length); i++)
            {
                int q = i;
                for(int j = 0; j < values.Length; j++)
                {
                    values[j] = (q & 1) != 0;
                    truthTable[i,j] = values[j];
                    q >>= 1;
                }
                truthTable[i, values.Length] = EvaluateBooleanExpression(expression, variables, values);
            }
            ResultSDNF.Text = "СДНФ: " + ToSDNF(truthTable, variables);
            ResultSKNF.Text = "СКНФ: " + ToSKNF(truthTable, variables);
        }

        private void LoadOperands(List<string> variables)
        {
            Operands.Children.Clear();
            listOperands.Clear();
            foreach (string variable in variables)
            {
                Operands.Children.Add(new Label() { Content = variable + "=", VerticalAlignment = VerticalAlignment.Center });
                var elem = new TextBox() { Height = 20, Width = 40, Text = "False" };
                listOperands.Add(elem);
                Operands.Children.Add(elem);
            }
        }

        private void Complete(object sender, RoutedEventArgs e)
        {
            string expression = Validate(Content.Text.ToString());
            var variables = GetVariables(expression).ToList();
            bool[] values = new bool[variables.Count];
            for (int i = 0; i < listOperands.Count; i++)
            {
                values[i] = listOperands[i].Text is "1" or "true" or "True";
            }
            Result.Text = "Значение логического выражения: " + EvaluateBooleanExpression(expression, variables, values);
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}