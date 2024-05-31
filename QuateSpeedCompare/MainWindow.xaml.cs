using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
//using Microsoft.Win32;

namespace QuateSpeedCompare
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public SeriesCollection ChartData { get; set; } = new SeriesCollection();

        public MainWindow()
        {
            InitializeComponent();
            /*// 创建一个 LineSeries 对象，并将其添加到 ChartData 中

            //初始化数据源
            List<double> dataList1 = new List<double> { 10, 20, 30, 40, 50 };
            List<double> dataList2 = new List<double> { 5, 15, 25, 35, 45 };

            //创建初始的LineSeries
            var lineSeries1 = new LineSeries
            {
                Title = "Series 1",
                Values = new ChartValues<double>(dataList1)
            };
            var lineSeries2 = new LineSeries
            {
                Title = "Series 2",
                Values = new ChartValues<double>(dataList2)
            };
            // 添加初始的 LineSeries 到 ChartData
            ChartData.Add(lineSeries1);
            ChartData.Add(lineSeries2);*/

            // 设置 CartesianChart 的数据源为 ChartData
            cartesianChart1.Series = ChartData;
        }

        /*//提取符合【9：30-10：30】且【股票代码为code】的TimeSpan*/
        private HashSet<TimeSpan> ReadTimeStampOfCSVFile(string filePath, string code)
        {
            var timestamps = new HashSet<TimeSpan>();
            //创建开始和结束时间并改成TimeSpan格式
            string begingin = "09:30:40.000";
            string endflagg = "15:00:00.000";
            string begingin1 = TimeSpan.ParseExact(begingin, "hh\\:mm\\:ss\\.fff", null).ToString("hh\\:mm\\:ss");
            string endflag1 = TimeSpan.ParseExact(endflagg, "hh\\:mm\\:ss\\.fff", null).ToString("hh\\:mm\\:ss");
            TimeSpan begin = TimeSpan.ParseExact(begingin1, "hh\\:mm\\:ss", null);
            TimeSpan endFlag = TimeSpan.ParseExact(endflag1, "hh\\:mm\\:ss", null);

            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(','); // 根据CSV文件的分隔符进行拆分
                    string temp;
                    // 将时间戳转换为TimeSpan类型
                    temp = TimeSpan.ParseExact(values[1], "hh\\:mm\\:ss\\.fff", null).ToString("hh\\:mm\\:ss");
                    TimeSpan timespan = TimeSpan.ParseExact(temp, "hh\\:mm\\:ss", null);
                    //如果时间戳大于10：31：00.000，直接返回,避免冗余扫描
                    if (timespan >= endFlag)
                    {
                        return timestamps;
                    }
                    //筛选符合【9：30-10：30】且【股票代码为code】的TimeSpan
                    if (timespan >= begin && values[2] == code)
                    {
                        timestamps.Add(timespan);
                    }
                }
            }
            return timestamps;
        }

        private List<double> ReturnDiff(string filepath, HashSet<TimeSpan> commonTimeStamps, string code)
        {
            if (commonTimeStamps is null)
            {
                throw new ArgumentNullException(nameof(commonTimeStamps));
            }

            List<double> diff = new List<double>();
            HashSet<TimeSpan> duplicates = new HashSet<TimeSpan>();
            //创建开始和结束时间并改成TimeSpan格式
            string begingin = "09:30:00.000";
            string endflagg = "15:00:00.000";
            string begingin1 = TimeSpan.ParseExact(begingin, "hh\\:mm\\:ss\\.fff", null).ToString("hh\\:mm\\:ss");
            string endflag1 = TimeSpan.ParseExact(endflagg, "hh\\:mm\\:ss\\.fff", null).ToString("hh\\:mm\\:ss");
            TimeSpan begin = TimeSpan.ParseExact(begingin1, "hh\\:mm\\:ss", null);
            TimeSpan endFlag = TimeSpan.ParseExact(endflag1, "hh\\:mm\\:ss", null);
            try
            {
                using (StreamReader reader = new StreamReader(filepath))
                {
                    reader.ReadLine();
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] fields = line.Split(',');
                        string temp;
                        // 将时间戳转换为TimeSpan类型
                        temp = TimeSpan.ParseExact(fields[1], "hh\\:mm\\:ss\\.fff", null).ToString("hh\\:mm\\:ss");
                        TimeSpan serverTime = TimeSpan.ParseExact(temp, "hh\\:mm\\:ss", null);

                        //如果时间戳大于10：31：00.000，直接返回,避免冗余扫描
                        if (serverTime >= endFlag)
                        {
                            return diff;
                        }

                        if (commonTimeStamps.Contains(serverTime) && fields[2] == code && !duplicates.Contains(serverTime))
                        {
                            diff.Add(double.Parse(fields[3]));
                            duplicates.Add(serverTime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while reading the file: " + ex.Message);
            }
            return diff;
        }

        //时间都减去最小值，消除服务器校准误差
        private List<double> EliminateServerErrors(List<double> diffs)
        {
            double min = diffs.Min();
            for (int i = 0; i < diffs.Count; i++)
            {
                diffs[i] = diffs[i] - min;
            }
            return diffs;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string code = textBox3.Text;
            string filepath1 = textBox1.Text;
            string filepath2 = textBox2.Text;
            string filepath3 = textBox4.Text;

            //读取第一个CSV文件
            HashSet<TimeSpan> timestamps1 = ReadTimeStampOfCSVFile(filepath1, code);

            //读取第二个CSV文件
            HashSet<TimeSpan> timestamps2 = ReadTimeStampOfCSVFile(filepath2, code);

            //读取第三个CSV文件
            HashSet<TimeSpan> timestamps3 = ReadTimeStampOfCSVFile(filepath3, code);

            //取时间戳的交集,即 到此步结束就取完了两个CSV文件的交集
            HashSet<TimeSpan> commonTimestamps1 = timestamps1.Intersect(timestamps2).ToHashSet();
            HashSet<TimeSpan> commonTimestamps = commonTimestamps1.Intersect(timestamps3).ToHashSet();
           /* if (commonTimestamps.Count == 0) {
                Window window = new Window {
                    Title = "提示",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen, // 设置弹窗居中显示
                    Content = "   三个数据源中并不含有该支股票的公共部分,请重新输入其它股票代码"
                };
                window.Show();
                return;
            }*/

            //提取filepath1的满足条件的diff
            List<double> diff1 = ReturnDiff(filepath1, commonTimestamps, code);

            //提取filepath2的满足条件的diff
            List<double> diff2 = ReturnDiff(filepath2, commonTimestamps, code);

            List<double> diff3 = ReturnDiff(filepath3, commonTimestamps, code);

            //消除服务器校准误差
            diff1 = EliminateServerErrors(diff1);
            diff2 = EliminateServerErrors(diff2);
            diff3 = EliminateServerErrors(diff3);


            //消除前22个数据，使可视化展示对比得更加直观
            //diff1.RemoveRange(0, 21);
            //diff2.RemoveRange(0, 21);
            //diff3.RemoveRange(0, 21);

          /*  //创建横坐标轴
            Axis axisX = new Axis
            {
                Title = "Time",
                Labels = (IList<string>)commonTimestamps
            };

            cartesianChart1.AxisX.Add(axisX);

            Axis axisY = new Axis
            {
                Title = "时间差",
            };
            cartesianChart1.AxisY.Add(axisY);*/
            //创建新的 LineSeries
            var newSeries1 = new LineSeries
            {
                Title = "安信",
                Values = new ChartValues<double>(diff1),
                Foreground = Brushes.Blue
            };
            var newSeries2 = new LineSeries
            {
                Title = "平安",
                Values = new ChartValues<double>(diff2),
                Foreground = Brushes.Red
            };

            var newSeries3 = new LineSeries
            {
                Title = "东吴",
                Values = new ChartValues<double>(diff3)
            };


            // 清除原有的 LineSeries
            ChartData.Clear();

            // 添加新的 LineSeries 到 ChartData
            ChartData.Add(newSeries1);
            ChartData.Add(newSeries2);
            ChartData.Add(newSeries3);  

            diff1.RemoveAll(x => x == 0);
            diff2.RemoveAll(x => x == 0);
            diff3.RemoveAll(x => x == 0);

            tB1.Text = diff1.Average().ToString();
            tB2.Text = FindMedian(diff1).ToString();
            tB3.Text = diff2.Average().ToString();
            tB4.Text = FindMedian(diff2).ToString();
            tB5.Text = diff3.Average().ToString();
            tB6.Text = FindMedian(diff3).ToString();

            tB7.Text = diff1.Max().ToString();
            tB8.Text = diff1.Where(x => x != 0).Min().ToString();
            tB10.Text = diff2.Max().ToString();
            tB11.Text = diff2.Where(x => x != 0).Min().ToString();
            tB13.Text = diff3.Max().ToString();
            tB14.Text = diff3.Where(x => x != 0).Min().ToString();

            tB9.Text = FindVariance(diff1).ToString();
            tB12.Text = FindVariance(diff2).ToString();
            tB15.Text = FindVariance(diff3).ToString();


        }


        private double FindVariance(List<double> diff)
        {
            double average = diff.Average();
            double sumOfSquares = diff.Sum(x => Math.Pow(x - average, 2));
            double variance = sumOfSquares / diff.Count;
            return variance;
        }

        //【浏览】 -》 打开文件资源管理器
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                textBox2.Text = openFileDialog.FileName;
            }
        }

        private void BrowseButton_Click2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                textBox4.Text = openFileDialog.FileName;
            }
        }
        //【浏览】 -》 打开文件资源管理器
        private void BrowseButton_Click1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                textBox1.Text = openFileDialog.FileName;
            }
        }

        //找到中位数
        private double FindMedian(List<double> diff)
        {
            double median;
            int count = diff.Count;

            var sortedNumbers = diff.OrderBy(x => x).ToList();

            if(count % 2 == 0)
            {
                median = (sortedNumbers[count / 2 - 1] + sortedNumbers[count / 2]) / 2.0;
            }
            else
            {
                median = sortedNumbers[count / 2];
            }
            return median;
        }
    }
}   

