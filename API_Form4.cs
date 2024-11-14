//폼2
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using Newtonsoft.Json;
using Renci.SshNet;
using Google.Cloud.Speech.V1;
using NAudio.Wave;
using System.Speech.Synthesis;


namespace Drive
{
    public partial class API_Form4 : Form
    {
        //OpenCV 변수
        private VideoCapture capture;
        private bool isCameraRunning = false;
        private CascadeClassifier faceCascade; // 얼굴 인식 모델
        private CascadeClassifier smileCascade;// 웃음 인식 모델
        private CascadeClassifier eyeCascade;// 눈 인식 모델
        //Rasberry PI 제어 관련 변수
        private SshClient sshClient; //SSH 클라이언트
        private const int ledPin = 17;
        private int smileCount = 0;
        private const int smileThreshold = 50;
        private bool brrcheck = false;
        private bool isBrrButtonPressed = false;
        private bool eyesDetected = false;
        private DateTime lastEyeDetectionTime;
        private const int EyeClosedThresholdSeconds = 3;
        //얼굴 및 사용자 관련 정보 변수
        private bool faceDetected = false;
        private static string name = "";
        private static string eid = "";
        private DirectionsService directionsService;


        public API_Form4()
        {
            InitializeComponent();
            directionsService = new DirectionsService();
        }
        //SSH 명령을 통해 Raspberry Pi에서 GPIO 핀 제어 명령을 실행
        private void ExecuteGpioCommand(string command)
        {
            try
            {
                using (var sshCommand = sshClient.CreateCommand(command))
                {
                    sshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Command execution failed: {ex.Message}");
            }
        }
        //Raspberry Pi 버저 울리기 위한 Python 스크립트 실행
        private void playbuzzer()
        {
            string buzzerCommand = "python3 /home/pi/webapps/buzer.py";
            ExecuteGpioCommand(buzzerCommand);
        }
        // StartVoiceRecognition 메서드: Google Speech-to-Text API를 사용하여 음성 인식 수행
        private async Task StartVoiceRecognition()
        {
            // Google API 인증 파일 경로 설정
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "coral-environs-440401-p8-52c240f1a8a5.json");

            // Google Speech-to-Text 클라이언트 생성
            var speechClient = SpeechClient.Create();
            var streamingCall = speechClient.StreamingRecognize(); // 스트리밍 인식 요청 객체 생성

            // Google Speech-to-Text 요청 설정
            await streamingCall.WriteAsync(new StreamingRecognizeRequest
            {
                StreamingConfig = new StreamingRecognitionConfig
                {
                    Config = new RecognitionConfig
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16, // 오디오 인코딩 방식
                        SampleRateHertz = 16000, // 오디오 샘플링 속도
                        LanguageCode = "ko-KR" // 한국어 인식
                    },
                    InterimResults = true // 일시적인 결과도 수신하도록 설정
                }
            });

            // NAudio 라이브러리를 사용하여 마이크 입력을 처리하고 Google API로 전송
            using (var waveIn = new WaveInEvent())
            {
                waveIn.DeviceNumber = 0; // 기본 마이크 장치 선택
                waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz, 모노 오디오 포맷 설정

                // 마이크 입력이 있을 때마다 호출되는 이벤트
                waveIn.DataAvailable += async (s, e) =>
                {
                    // 음성 데이터를 Google API로 전송
                    await streamingCall.WriteAsync(new StreamingRecognizeRequest
                    {
                        AudioContent = Google.Protobuf.ByteString.CopyFrom(e.Buffer, 0, e.BytesRecorded)
                    });
                };

                // 음성 인식 결과를 비동기로 확인하는 작업
                Task printResponses = Task.Run(async () =>
                {
                    // 스트리밍 응답을 순차적으로 처리
                    await foreach (var response in streamingCall.GetResponseStream())
                    {
                        // 응답 결과에서 첫 번째로 "시동 켜라"를 포함하는 전사본(Transcript) 찾기
                        var transcript = response.Results
                            .SelectMany(result => result.Alternatives)   // 각 result의 Alternatives를 하나의 컬렉션으로 병합
                            .Select(alternative => alternative.Transcript) // 각 alternative에서 Transcript 추출
                            .FirstOrDefault(transcript => transcript.Contains("시동 켜라")); // "시동 켜라"를 포함하는 첫 번째 전사본 찾기

                        // "시동 켜라" 명령어가 감지되었을 경우
                        if (transcript != null)
                        {
                            // GPIO 핀을 통해 LED 켜기 명령어 실행
                            ExecuteGpioCommand($"gpio -g write {ledPin} out");
                            ExecuteGpioCommand($"gpio -g write {ledPin} 1");

                            // 음성 안내 및 시동 관련 플래그 설정
                            Speaker();
                            eyesDetected = true;
                            brrcheck = true;

                            // 새 폼(API_Form1)을 열어 사용자 정보를 표시
                            this.Invoke(new Action(() =>
                            {
                                API_Form1 form1 = new API_Form1(name, eid);
                                form1.Show();
                            }));
                            return; // 작업 종료
                        }
                    }
                });

                // 마이크 입력 시작
                waveIn.StartRecording();

                // 음성 인식 결과가 완료될 때까지 대기
                await printResponses;

                // 마이크 입력 및 스트리밍 요청 종료
                waveIn.StopRecording();
                await streamingCall.WriteCompleteAsync(); // 스트리밍 종료
            }
        }
        private void Speaker()
        {
            // 비동기적으로 음성 합성 작업을 실행
            Task.Run(() =>
            {
                // SpeechSynthesizer 객체 생성
                using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
                {
                    try
                    {
                        // 음성 출력 속도 및 볼륨 설정
                        synthesizer.Rate = 0;
                        synthesizer.Volume = 100;

                        // 음성을 출력
                        synthesizer.Speak("안전 운행 하세요");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"음성 합성 오류: {ex.Message}");
                    }
                }
            });
        }
        private async void API_button_start_Click_1(object sender, EventArgs e)
        {
            // 카메라에서 현재 프레임을 캡처하여 비교
            var frame = new Mat();
            capture.Read(frame);
            if (!frame.Empty())
            {
                //JSON 경로
                string jsonPath = "Data.json";
                // 사원 이미지 경로와 관련 정보를 저장할 리스트와 딕셔너리 생성
                List<string> sourceImagePath = new List<string>();
                Dictionary<string, (string Name, string EID)> imagePathToName = new Dictionary<string, (string, string)>();
                try
                {
                    //JSON 파일을 UTF-8로 읽어와서 사원 데이터 역직렬화
                    string jsonString = File.ReadAllText(jsonPath, Encoding.UTF8);
                    //JSON 데이터를 EmployeesData 객체로 변환
                    var data = JsonConvert.DeserializeObject<EmployeesData>(jsonString);
                    //각 사원 정보에서 이미지 경로와 이름, ID를 저장
                    foreach (var employee in data.Employees)
                    {
                        if (!string.IsNullOrEmpty(employee.Image))
                        {
                            //이미지 경로를 리스트에 추가
                            sourceImagePath.Add(employee.Image);
                            // 이미지 경로를 키로 하고 이름과 ID를 값으로 저장
                            imagePathToName[employee.Image] = (employee.Name, employee.EmployeesID);
                        }
                    }
                    string matchedImagePath = null;
                    bool check = await API_face.CompareFacesAsync(sourceImagePath.ToArray(), frame);
                    if (check)
                    {
                        StartVoiceRecognition();
                        matchedImagePath = API_face.LastMatchedImagePath;
                        if (matchedImagePath != null && imagePathToName.ContainsKey(matchedImagePath))
                        {
                            var (matchedName, matchedEID) = imagePathToName[matchedImagePath];
                            MessageBox.Show($"이름 : {matchedName} 코드 : {matchedEID} 님이 확인되었습니다.");
                            name = matchedName;
                            eid = matchedEID;
                        }
                    }
                    else
                    {
                        MessageBox.Show("너누구야");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"JSON 파일 읽다가 오류 : {ex.Message}");
                }

            }
            else
            {
                MessageBox.Show("웹캠에서 이미지를 캡처하지 못했습니다.");
            }


        }
        private void API_Form2_Load(object sender, EventArgs e)
        {
            API_button_start.Enabled = false;
            try
            {
                string host = "192.168.0.201"; // 라즈베리파이의 IP 주소
                string username = "pi"; // 라즈베리파이의 사용자명
                string password = "moble"; // 라즈베리파이의 비밀번호

                sshClient = new SshClient(host, username, password);
                sshClient.Connect();
                if (sshClient.IsConnected)
                    MessageBox.Show("Connected to Raspberry Pi!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
            ExecuteGpioCommand($"gpio -g mode {ledPin} out");
            ExecuteGpioCommand($"gpio -g write {ledPin} 0"); // LED 끄기
            // 웹캠 초기화
            capture = new VideoCapture(0); // 0번 카메라 (기본 웹캠)
            if (!capture.IsOpened())
            {
                MessageBox.Show("카메라를 열 수 없습니다.");
                return;
            }

            // 얼굴 검출을 위한 CascadeClassifier 로드
            faceCascade = new CascadeClassifier("haarcascade_frontalface_default.xml");
            if (faceCascade.Empty())
            {
                MessageBox.Show("얼굴 인식 모델을 로드할 수 없습니다.");
                return;
            }
            smileCascade = new CascadeClassifier("haarcascade_smile.xml");
            if (smileCascade.Empty())
            {
                MessageBox.Show("웃음 인식 모델을 로드할 수 없습니다.");
                return;
            }
            eyeCascade = new CascadeClassifier("haarcascade_eye.xml");
            if (smileCascade.Empty())
            {
                MessageBox.Show("눈 인식 모델을 로드할 수 없습니다.");
                return;
            }


            isCameraRunning = true;
            // 음성 명령 기능 시작

            Task.Run(() => ProcessCamera());
        }
        private void ProcessCamera()
        {
            while (isCameraRunning)
            {
                using (var frame = new Mat())
                {
                    capture.Read(frame);
                    if (frame.Empty())
                        continue;

                    var grayFrame = new Mat();
                    Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);

                    var faces = faceCascade.DetectMultiScale(grayFrame, 1.1, 4);
                    if (faces.Length > 0)
                    {
                        faceDetected = true;
                        var largestFace = faces.OrderByDescending(face => face.Width * face.Height).First();
                        Cv2.Rectangle(frame, largestFace, Scalar.Red, 2);

                        var faceROI = new Mat(grayFrame, largestFace);
                        var smiles = smileCascade.DetectMultiScale(faceROI, 1.8, 30);
                        if (smiles.Length > 0)
                        {
                            // 웃음이 감지되었을 때 smileCount 증가
                            smileCount++;
                            if (smileCount >= smileThreshold)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    API_button_start.Enabled = true;
                                }));
                                smileCount = 0; // 카운터 초기화
                            }
                        }
                        else
                        {
                            smileCount = 0; // 웃음이 감지되지 않으면 카운터 초기화
                        }
                        foreach (var smile in smiles)
                        {
                            var smileRect = new Rect(largestFace.X + smile.X, largestFace.Y + smile.Y, smile.Width, smile.Height);
                            Cv2.Rectangle(frame, smileRect, Scalar.Green, 2);
                        }
                        var eyes = eyeCascade.DetectMultiScale(faceROI, 1.1, 20);
                        if (eyes.Length > 0 && brrcheck == true)
                        {
                            eyesDetected = true;
                            foreach (var eye in eyes)
                            {
                                var eyeRect = new Rect(largestFace.X + eye.X, largestFace.Y + eye.Y, eye.Width, eye.Height);
                                Cv2.Rectangle(frame, eyeRect, Scalar.Red, 2);
                            }
                            lastEyeDetectionTime = DateTime.Now;  // 눈을 감지한 시간 기록
                        }
                        else
                        {
                            // 눈이 감지되지 않으면 눈 감기 상태로 설정
                            if (faceDetected && brrcheck && !eyesDetected && (DateTime.Now - lastEyeDetectionTime).TotalSeconds >= EyeClosedThresholdSeconds)
                            {

                                this.Invoke(new Action(() =>
                                {
                                    playbuzzer();

                                }));
                                eyesDetected = false;  // 경고 메시지를 한 번 출력한 후 눈 감기 상태를 초기화
                            }
                            else
                            {
                                eyesDetected = false;  // 눈을 감은 상태로 설정X
                            }
                        }
                    }
                    var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);

                    this.Invoke(new Action(() =>
                    {
                        pictureBox1.Image?.Dispose();
                        pictureBox1.Image = bitmap;
                    }));
                }

            }

        }

        private async void btnShowTotalDistanceClick_Click(object sender, EventArgs e)
        {
            // 사용자가 얼굴 인식 후 얻은 eid (사원 코드)를 이용해 총 이동 거리 조회
            if (string.IsNullOrEmpty(eid))
            {
                MessageBox.Show("먼저 얼굴 인식이 필요합니다.");
                return;
            }

            try
            {
                // 총 이동 거리 조회
                double totalDistance = await directionsService.GetTotalDistanceAsync(eid);

                // 총 이동 거리를 메시지 박스로 출력
                MessageBox.Show($"사원 코드: {eid}\n총 이동 거리: {totalDistance} km",
                                "총 이동 거리",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
            }
        }
    }
}


