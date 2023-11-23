using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hirata_LoadPort_driver
{
    public enum DigitalValue
    {
        Off = 0,
        On = 1
    };

    // 응답코드/////////////////////////////////////////////
    public class Response_List
    {
        private string _response_Name = "";

        public string response_code
        {
            get
            {
                if (string.IsNullOrEmpty(_response_Name))
                {
                    _response_Name = "Response name is missing";
                }

                return _response_Name;
            }
            set
            {
                if      (value == "01") _response_Name = "CheckSum error";                
                else if (value == "02") _response_Name = "Command error";
                else if (value == "04") _response_Name = "Interlock";
                else if (value == "05") _response_Name = "Alarm is occurring";
                else if (value == "06") _response_Name = "Command processing";
                else if (value == "07") _response_Name = "Mode error";
                else if (value == "08") _response_Name = "Mapping error";               
            }
        }
    }
    ////////////////////////////////////////////////////////

    // 인터록 리스트/////////////////////////////////////////
    public class Interlock_List
    {
        private string _interlock_Name = "";

        public string interlock_code
        {
            get
            {
                if (string.IsNullOrEmpty(_interlock_Name))
                {
                    _interlock_Name = "Interlock name is missing";
                }

                return _interlock_Name;
            }
            set
            {
                if      (value == "01") _interlock_Name = "상위 컴퓨터로부터의 AVAILABLE 신호가 OFF 되어 있음";                
                else if (value == "10") _interlock_Name = "FOUP이 미안착 상태 또는 안착 상태에 이상이 발생";
                else if (value == "12") _interlock_Name = "장치 상태가 원점 위치 상태가 아님";
                else if (value == "13") _interlock_Name = "장치 상태가 로딩 완료 위치 상태가 아님";
                else if (value == "14") _interlock_Name = "클램프 기구가 클램프 완료(FOUP 고정) 상태가 아님";
                else if (value == "15") _interlock_Name = "독 슬라이드 기구가 독 완료(FOUP 전) 상태가 아님";
                else if (value == "16") _interlock_Name = "흡착 기구가 도어 흡착 상태가 아님";
                else if (value == "17") _interlock_Name = "래치 개폐 기구가 언래치(도어록) 상태가 아님";
                else if (value == "18") _interlock_Name = "도어 퇴피 기구가 도어 오픈 상태가 아님";
                else if (value == "19") _interlock_Name = "맵핑 개시 상태가 아님";
                else if (value == "1A") _interlock_Name = "맵핑 대피 기구가 맵핑 개시 상태가 아님";
                else if (value == "1C") _interlock_Name = "도어 승강 기구가 도어 개폐 위치에 없음";
                else if (value == "1D") _interlock_Name = "맵핑 승강 기구가 맵핑 개시로부터 종료 위치의 범위에 없음";
                else if (value == "1E") _interlock_Name = "독 슬라이드 기구가 언독 완료 상태가 아님";                
            }
        }
    }
    ////////////////////////////////////////////////////////

    // 에러 코드////////////////////////////////////////////
    public class Error_List
    {
        private string _error_Name = "";

        public string error_code
        {
            get
            {
                if (string.IsNullOrEmpty(_error_Name))
                {
                    _error_Name = "Error code name is missing";
                }

                return _error_Name;
            }
            set
            {
                if      (value == "00") _error_Name = "정상";
                else if (value == "10") _error_Name = "클램프 타임 오버";
                else if (value == "11") _error_Name = "언클램프 타임 오버";
                else if (value == "12") _error_Name = "독 타임 오버";
                else if (value == "13") _error_Name = "언독 타임 오버";
                else if (value == "14") _error_Name = "래치 타임 오버";
                else if (value == "15") _error_Name = "언래치 타임 오버";
                else if (value == "16") _error_Name = "흡착 타임 오버";
                else if (value == "17") _error_Name = "흡착 해제 타임 오버";
                else if (value == "18") _error_Name = "도어 오픈 타임 오버";
                else if (value == "19") _error_Name = "도어 클로즈 타임 오버";
                else if (value == "1A") _error_Name = "맵핑 포워드 타임 오버";
                else if (value == "1B") _error_Name = "맵핑 리턴 타임 오버";
                else if (value == "1F") _error_Name = "통신 에러";
                else if (value == "20") _error_Name = "원점 복귀 타임 오버";
                else if (value == "21") _error_Name = "로딩 타임 오버";
                else if (value == "22") _error_Name = "언로딩 타임 오버";
                else if (value == "23") _error_Name = "위치 결정 타임 오버";
                else if (value == "28") _error_Name = "승강축 도어 개폐 위치 이동타임 오버";
                else if (value == "29") _error_Name = "승강축 맵핑 개시 위치 이동타임 오버";
                else if (value == "2A") _error_Name = "승강축 맵핑 종료 위치 이동타임 오버";
                else if (value == "2B") _error_Name = "승강축 로드 위치 이동 타임 오버";
                else if (value == "30") _error_Name = "맵핑 캘리브레이션 에러1";
                else if (value == "31") _error_Name = "맵핑 캘리브레이션 에러2";
                else if (value == "32") _error_Name = "맵핑 캘리브레이션 에러3";
                else if (value == "36") _error_Name = "맵핑 캘리브레이션 에러4";
                else if (value == "37") _error_Name = "맵핑 캘리브레이션 에러4";
                else if (value == "40") _error_Name = "맵핑 데이터 이상";
                else if (value == "41") _error_Name = "모드 변환 이상";
                else if (value == "50") _error_Name = "Z 승강축 캘리브레이션 에러1";
                else if (value == "51") _error_Name = "Z 승강축 캘리브레이션 에러2";
                else if (value == "52") _error_Name = "Z 승강축 캘리브레이션 에러3";
                else if (value == "53") _error_Name = "Z 승강축 캘리브레이션 에러4";
                else if (value == "54") _error_Name = "Z 승강축 캘리브레이션 에러5";
                else if (value == "70") _error_Name = "클램프/언클램프 센서가 동시에 검출 상태가 됨";
                else if (value == "71") _error_Name = "독/언독 센서가 동시에 검출 상태가 됨";
                else if (value == "72") _error_Name = "래치/언래치 센서가 동시에검출 상태가 됨";
                else if (value == "73") _error_Name = "도어 오픈/클로즈 센서가 동시에 검출 상태가 됨";
                else if (value == "74") _error_Name = "맵핑 포워드/리턴 센서가 동시에 검출 상태가 됨";
                else if (value == "77") _error_Name = "도어 상한/하한 센서가 동시에 검출 상태가 됨";
                else if (value == "A0") _error_Name = "도어 유지 중 유지 상태가 변화됨";
                else if (value == "A1") _error_Name = "독 출력 중 웨이퍼 돌출 센서 검출 상태가 됨";
                else if (value == "A2") _error_Name = "FOUP 재치 센서 상태가 비정상";
                else if (value == "A3") _error_Name = "FOUP 재하 센서 상태가 비정상";
                else if (value == "A5") _error_Name = "공급 에어 압 상태가 비정상";
                else if (value == "B0") _error_Name = "상위 장치 이상(PIO 입력 없음)";
                else if (value == "C0") _error_Name = "파라미터 이상";
                else if (value == "E0") _error_Name = "FAN 정지 알람";
                else if (value == "E3") _error_Name = "전원 전압 저하";
                else if (value == "FE") _error_Name = "독 손끼임 검출";
            }
        }
    }
    ////////////////////////////////////////////////////////

    public struct LOADPORT_STATUS
    {
        // LoadPort status
        public string sR_ErrorSts;              // 에러 상태
        public string sR_Mode;                  // 모드
        public string sR_DeviceSts;             // 장치 상태
        public string sR_OperationSts;          // 동작 상태
        public string[] sR_ErrorCode;           // 에러 코드
        public string sR_ContainerSts;          // 용기 상태
        public string sR_ClampPosition;         // 클램프 위치
        public string sR_DoorLatchPosition;     // 도어 래치 위치
        public string sR_AdsorptionSts;         // 흡착 상태
        public string sR_DoorPosition;          // 도어 위치
        public string sR_WaferProtrusionSns;    // 웨이퍼 돌출 센서
        public string sR_ElevatorAxisPosition;  // 승강축 위치
        public string sR_DockPosition;          // 독 위치
        public string sR_MappWaitPosition;      // 맵핑 대기 위치
        public string sR_MappSts;               // 맵핑 상태
        public string sR_Type;                  // 기종
    }

    public struct LED_STATUS
    {
        // LED status
        public string sR_Presence;              // PRESENCE LED의 상태
        public string sR_Placement;             // PLACEMENT LED의 상태
        public string sR_Load;                  // LOAD LED의 상태
        public string sR_Unload;                // UNLOAD LED의 상태
        public string sR_OpAccess1;             // OperatorAccess1의 상태
        public string sR_Status1;               // STATUS1 LED의 상태
        public string sR_Status2;               // STATUS2 LED의 상태
        public string sR_OpAccess2;             // OperatorAccess2의 상태
    }

    public class LoadPortDefine
    {
        public const int RS_NUL = 0x00;
        public const int RS_SOH = 0x01;
        public const int RS_STX = 0x02;
        public const int RS_ETX = 0x03;
        public const int RS_LF = 0x0A;
        public const int RS_CR = 0x0D;
        public const int RS_NAK = 0x15;
        public const int RS_SP = 0x20;

        public const int LOADPORT_MAX = 1;

        public static string sCode = "00";        
        public static string sAdr = "00";


        public static string sInterlockName;
        public static string sResponseName;
        public static string sErrorCodeName;
    }
}
