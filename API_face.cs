//얼굴인식 api
using System;
using System.Threading.Tasks;
using System.IO;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Forms;
using Newtonsoft.Json;
namespace Drive
{
    //얼굴 인식 api를 가져오는 클래스
    internal class API_face
    {
        // api 키 2종
        private static readonly string accessKeyId = "AKIAVRUVTFDA7FFIZSDK";
        private static readonly string secretAccessKey = "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";
        // api 지역
        private static readonly RegionEndpoint region = RegionEndpoint.APNortheast2;
        //api 객체
        private static readonly AmazonRekognitionClient rekognitionClient = new AmazonRekognitionClient(accessKeyId, secretAccessKey, region);
        //이미지 경로를 가져오는 속성
        public static string LastMatchedImagePath { get; private set; }
        //얼굴 비교하는 비동기 함수
        public static async Task<bool> CompareFacesAsync(string[] sourceImagePaths, Mat webcamImage)
        {
            
            try
            {
                // OpenCV Mat 객체에서 이미지 바이트로 변환
                byte[] webcamImageData;
                using (var ms = new MemoryStream())
                {
                    var bitmap = BitmapConverter.ToBitmap(webcamImage); // OpenCV Mat을 Bitmap으로 변환
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    webcamImageData = ms.ToArray();
                }
                //Rekognition에 전달할 Image 객체 생성
                var targetImage = new Image
                {
                    Bytes = new MemoryStream(webcamImageData)
                };
                //Json 파일에서 사원 이미지 목록 가져오기
                var jsonString = File.ReadAllText("Data.json");
                var employeesData = JsonConvert.DeserializeObject<EmployeesData>(jsonString);
                //매칭 여부 플래그
                bool matchFound = false;
                foreach (var employee in employeesData.Employees)
                {
                    
                    var sourceImagePath = employee.Image;
                    if (matchFound) break;
                    try
                    {
                        //사원 이미지를 Rekognition의 SourceImage 형식으로 변환
                        var sourceImage = new Image
                        {
                            Bytes = new MemoryStream(File.ReadAllBytes(sourceImagePath))
                        };
                        //얼굴 비교 요청 설정
                        var request = new CompareFacesRequest
                        {
                            SourceImage = sourceImage,
                            TargetImage = targetImage,
                            SimilarityThreshold = 70F
                        };
                        //얼굴 비교 요청 실행
                        var response = await rekognitionClient.CompareFacesAsync(request);
                        //얼굴 일치 확인
                        foreach (var match in response.FaceMatches)
                        {
                            //유사도 90% 이상일 때 매칭 성공
                            if (match.Similarity >= 90F)
                            {
                                LastMatchedImagePath = sourceImagePath;
                                MessageBox.Show("일치하는 얼굴 발견");
                                matchFound = true;
                                break;
                            }

                        }
                        //매칭된 얼굴 찾을 시 종료
                        if (matchFound) break;

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                        return false;
                    }
                }
                if (!matchFound)
                {
                    MessageBox.Show("일치하는 얼굴 없음");
                }
                return matchFound;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
                return false;
            }
        }
    }
}

