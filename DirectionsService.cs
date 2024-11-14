using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MySqlConnector;

namespace Drive
{
    //TMAP 및 카카오 API를 사용해 경로 정보 및 주소를 가져오는 서비스 클래스
    public class DirectionsService
    {
        //TMAP API를 통해 두 지점 간의 거리와 예상 소요 시간 계산
        public async Task<(double Distance, string TimeFormat)> GetDistanceAsync(double startLat, double startLng, double endLat, double endLng, string EmployeeCode, string EmployeeName)
        {
            using (HttpClient client = new HttpClient())
            {
                //TMAP 경로 요청 URL 설정 ( 버전 1, JSON 포맷으로 결과 수신)
                string requestUrl = $"https://apis.openapi.sk.com/tmap/routes?version=1&format=json";
                //TMAP API에 전달할 요청 본문 설정 (출발지 및 도착지 좌표와 기타 옵션)
                var requestBody = new
                {
                    startX = startLng.ToString(), // 출발지 경도(X좌표)
                    startY = startLat.ToString(), // 출발지 위도(Y좌표)
                    endX = endLng.ToString(), // 도착지 경도 (X 좌표)
                    endY = endLat.ToString(), // 도착지 위도 (Y 좌표)
                    reqCoordType = "WGS84GEO", // 요청 좌표 타입 (WGS84GEO : GPS 좌표 시스템)
                    resCoordType = "WGS84GEO", // 응답 좌표 타입 (WGS84GEO : GPS 좌표 시스템)
                    angle = "172", // 출발지 방향 (차량의 진행 방향, 임의 설정 가능)
                    searchOption = "0", // 경로 탐색 옵션 ( 0 : 추천 경로 )
                    trafficInfo = "N" // 교통 정보 사용 여부 ( N : 미사용 )
                };
                
                //요청 본문을 JSON 형식으로 직렬화하여 HTTP 요청의 내용으로 설정
                var jsonRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");
                // HTTP 요청 헤더에 TMAP API 키 설정
                client.DefaultRequestHeaders.Add("appkey", apiKey);
                // POST 요청을 통해 경로 계산 요청 전송
                HttpResponseMessage response = await client.PostAsync(requestUrl, content);
                // 응답이 성공적인 경우 ( StatusCode가 2XX)
                if (response.IsSuccessStatusCode)
                {
                    // 응답 데이터를 문자열로 읽어오기
                    string responseData = await response.Content.ReadAsStringAsync();
                    // 응답 JSON 문자열을 파싱하여 JObject로 변환
                    var data = JObject.Parse(responseData);
                    // 응답 JSON 데이터에 "features" 필드가 존재하고 값이 있는 경우
                    if (data["features"] != null && data["features"].HasValues)
                    {
                        // 경로 정보가 포함된 "features"의 첫 번째 요소의 "properties" 필드 가져오기
                        var properties = data["features"][0]["properties"];
                        double distance = properties["totalDistance"].ToObject<double>() / 1000; // km 단위로 변환
                        double durationSeconds = properties["totalTime"].ToObject<double>();
                        double durationMinutes = durationSeconds / 60.0;
                        int hours = (int)durationMinutes / 60;
                        int minutes = (int)durationMinutes % 60;
                        string timeFormat = $"{hours} 시간 {minutes} 분";

                        // 주소 가져오기
                        string destinationName = await GetAddressFromCoordinatesAsync(endLat, endLng);

                        // 데이터베이스에 저장
                        await InsertDriveInfoAsync(EmployeeCode, EmployeeName, startLat, startLng, endLat, endLng, distance, durationMinutes, destinationName);

                        return (distance, timeFormat);
                    }
                    else
                    {
                        throw new Exception("경로를 찾을 수 없습니다.");
                    }
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API 요청 오류: {response.StatusCode} - {response.ReasonPhrase}\n내용: {errorResponse}");
                }
            }
        }
        // MySQL 데이터베이스에 운전 정보 삽입
        private async Task InsertDriveInfoAsync(string EmployeeCode, string EmployeeName, double startLat, double startLng, double endLat, double endLng, double distance, double durationMinutes, string destinationName)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO drive_info (EmployeeID,Employee_name,departure_x, departure_y, destination_x, destination_y, estimated_distance, estimated_time, destination_name)
                    VALUES (@EmployeeCode,@EmployeeName,@startLng, @startLat, @endLng, @endLat, @distance, @durationMinutes, @destinationName)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmployeeCode", EmployeeCode);
                    command.Parameters.AddWithValue("@EmployeeName", EmployeeName);
                    command.Parameters.AddWithValue("@startLng", startLng);
                    command.Parameters.AddWithValue("@startLat", startLat);
                    command.Parameters.AddWithValue("@endLng", endLng);
                    command.Parameters.AddWithValue("@endLat", endLat);
                    command.Parameters.AddWithValue("@distance", distance);
                    command.Parameters.AddWithValue("@durationMinutes", durationMinutes);
                    command.Parameters.AddWithValue("@destinationName", destinationName);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        //좌표를 주소로 변환 
        public async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            using (var client = new HttpClient())
            {
                string apiKey // 카카오 REST API 키
                string requestUrl = $"https://dapi.kakao.com/v2/local/geo/coord2address.json?x={longitude}&y={latitude}&input_coord=WGS84";

                client.DefaultRequestHeaders.Add("Authorization", $"KakaoAK {apiKey}");

                HttpResponseMessage response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(content);

                if (json["documents"] != null && json["documents"].HasValues)
                {
                    return json["documents"][0]["address"]["address_name"].ToString();
                }
                else
                {
                    return "주소를 찾을 수 없습니다.";
                }
            }
        }
        // 특정 사원의 총 이동 거리 계산
        public async Task<double> GetTotalDistanceAsync(string EmployeeCode)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // EmployeeCode에 해당하는 모든 estimated_distance 값을 합산하는 쿼리
                string query = @"
            SELECT SUM(estimated_distance) AS total_distance
            FROM drive_info
            WHERE EmployeeID = @EmployeeCode
            GROUP BY EmployeeID;";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmployeeCode", EmployeeCode);

                    // ExecuteScalarAsync는 쿼리 결과의 첫 번째 행의 첫 번째 열을 반환
                    var result = await command.ExecuteScalarAsync();

                    // 결과가 null인 경우 0을 반환
                    return result != DBNull.Value ? Convert.ToDouble(result) : 0;
                }
            }
        }

    }
}
