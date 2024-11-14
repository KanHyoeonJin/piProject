using System;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.MapProviders;
namespace Drive
{
    public partial class API_Form1 : Form
    {
        private GMapControl gMapControl;
        private PointLatLng currentLocation = new PointLatLng(36.8108, 127.1460);
        private DirectionsService directionsService = new DirectionsService();
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }

        public API_Form1(string name, string code)
        {
            InitializeComponent();
            InitializeMap();
            EmployeeName = name;
            EmployeeCode = code;
        }

        private void InitializeMap()
        {
            gMapControl = new GMapControl();
            gMapControl.Dock = DockStyle.Fill;
            gMapControl.MapProvider = GMapProviders.GoogleMap;
            gMapControl.Position = new PointLatLng(36.8109, 127.1465);
            gMapControl.MinZoom = 1;
            gMapControl.MaxZoom = 18;
            gMapControl.Zoom = 15;
            gMapControl.CanDragMap = true;
            gMapControl.MouseClick += CalculateDistanceButton_Click;
            gMapControl.ShowCenter = false;
            this.Controls.Add(gMapControl);
        }

        private async void CalculateDistanceButton_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PointLatLng point = gMapControl.FromLocalToLatLng(e.X, e.Y);
                double startLat = 36.8109;
                double startLng = 127.1465;

                try
                {
                    // 거리 및 소요 시간 계산
                    var (distance, duration) = await directionsService.GetDistanceAsync(startLat, startLng, point.Lat, point.Lng, EmployeeCode, EmployeeName);
                    // 좌표를 주소로 변환
                    string address = await directionsService.GetAddressFromCoordinatesAsync(point.Lat, point.Lng);
                    MessageBox.Show($"사원 코드: {EmployeeCode}\n사원 이름: {EmployeeName}\n" +
                                    $"천안역에서 클릭한 위치까지의 거리 : {distance} km\n" +
                                    $"소요 시간 : {duration}\n" +
                                    $"주소 : {address}");

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void API_Form1_Load(object sender, EventArgs e)
        {

        }
    }
}