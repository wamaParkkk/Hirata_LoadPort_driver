using System;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Xml.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace Hirata_LoadPort_driver
{
    public class LoadPortClass : LoadPortDefine
    {
        private static Thread drvThread;
        private static bool _continue;
        private static bool _bSet_flag;

        private static SerialPort _serialPort;

        public static LOADPORT_STATUS loadPortStatus;
        public static LED_STATUS ledStatus;

        public static Response_List response_List;
        public static Interlock_List interlock_List;
        public static Error_List error_List;        

        public static void LoadPort_Init()
        {
            drvThread = null;
            bool bRtn;

            loadPortStatus.sR_ErrorCode = new string[2];

            loadPortStatus.sR_ErrorSts = string.Empty;
            loadPortStatus.sR_Mode = string.Empty;
            loadPortStatus.sR_DeviceSts = string.Empty;
            loadPortStatus.sR_OperationSts = string.Empty;
            loadPortStatus.sR_ErrorCode[0] = string.Empty;
            loadPortStatus.sR_ErrorCode[1] = string.Empty;                       
            loadPortStatus.sR_ContainerSts = string.Empty;
            loadPortStatus.sR_ClampPosition = string.Empty;            
            loadPortStatus.sR_DoorLatchPosition = string.Empty;
            loadPortStatus.sR_AdsorptionSts = string.Empty;
            loadPortStatus.sR_DoorPosition = string.Empty;
            loadPortStatus.sR_WaferProtrusionSns = string.Empty;
            loadPortStatus.sR_ElevatorAxisPosition = string.Empty;
            loadPortStatus.sR_DockPosition = string.Empty;
            loadPortStatus.sR_MappWaitPosition = string.Empty;
            loadPortStatus.sR_MappSts = string.Empty;
            loadPortStatus.sR_Type = string.Empty;

            bRtn = _DRV_INIT();
            if (bRtn)
            {
                _continue = true;
                _bSet_flag = false;

                drvThread = new Thread(_Load_port_thread);
                drvThread.Start();
            }
            else
            {
                Global.EventLog("Load port driver initialization fail");
                _DRV_CLOSE();
            }
        }

        private static bool _DRV_INIT()
        {
            if (_InitPortInfo())
            {
                Global.EventLog("Acquisition of serial communication port information is completed");
            }
            else
            {
                return false;
            }

            if (_PortOpen())
            {
                Global.EventLog("Serial port opened successfully");                
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool _InitPortInfo()
        {
            _serialPort = new SerialPort();

            string sTmpData;
            string FileName = "LPPortInfo.txt";

            try
            {
                if (File.Exists(Global.BasePath + FileName))
                {
                    byte[] bytes;
                    using (var fs = File.Open(Global.BasePath + FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bytes = new byte[fs.Length];
                        fs.Read(bytes, 0, (int)fs.Length);
                        sTmpData = Encoding.Default.GetString(bytes);

                        char sp = ',';
                        string[] spString = sTmpData.Split(sp);
                        for (int i = 0; i < spString.Length; i++)
                        {
                            string sPortName = spString[0];
                            int iBaudRate = int.Parse(spString[1]);
                            int iDataBits = int.Parse(spString[2]);
                            int iStopBits = int.Parse(spString[3]);
                            int iParity = int.Parse(spString[4]);

                            _serialPort.PortName = sPortName;
                            _serialPort.BaudRate = iBaudRate;
                            _serialPort.DataBits = iDataBits;
                            _serialPort.StopBits = (StopBits)iStopBits;
                            _serialPort.Parity = (Parity)iParity;

                            _serialPort.ReadTimeout = 1000;
                            _serialPort.WriteTimeout = 1000;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (FileLoadException)
            {                
                return false;
            }
        }

        private static bool _PortOpen()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    if (port != "")
                    {
                        _serialPort.Open();
                        if (_serialPort.IsOpen)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                return false;
            }
            catch (IOException)
            {                
                return false;
            }
        }

        public static void _DRV_CLOSE()
        {
            _continue = false;
            
            if (drvThread != null)
            {
                drvThread.Abort();
                Global.EventLog("Load port thread abort");
            }            
            
            Global.EventLog("Load port driver close");
        }

        #region PARAMETER READ THREAD
        private static void _Load_port_thread()
        {
            try
            {
                while (_continue)
                {
                    if (!_bSet_flag)
                    {
                        _Parameter_read();
                        _Led_Status_read();

                        Thread.Sleep(10);
                    }                    
                }
            }
            catch (ThreadStateException ex)
            {
                Global.EventLog(string.Format("Load port thread error : {0}", ex));
            }
        }
        
        private static void _Parameter_read()
        {            
            try
            {
                // Status request
                string sData = string.Empty;
                sData += sCode;
                sData += sAdr;
                sData += "GET:STAS";
                sData += ";";
                string _chksumData = Global.Checksum(sData);

                string send_Command = string.Format("{0}{1}{2}{3}", Convert.ToChar(RS_SOH), sData, _chksumData, Convert.ToChar(RS_CR));
                _serialPort.Write(send_Command);                
                Global.EventLog("Send : " + send_Command);                

                Thread.Sleep(20);

                string readData = _serialPort.ReadTo(Convert.ToChar(RS_CR).ToString());
                Global.EventLog("Recv : " + readData);
                _serialPort.DiscardInBuffer();

                if (_DataCheck(readData) == 0)
                {
                    int nSize = readData.Length;
                    int bufPos = 0;
                    char[] charArray;
                    charArray = new char[nSize];
                    for (int i = 0; i < nSize; i++)
                    {
                        charArray[bufPos] = readData[i];
                        bufPos++;
                    }

                    if ((charArray[13] == Convert.ToChar(0x2F)) && (charArray[34] == Convert.ToChar(0x3B)))
                    {
                        _STATUS_PARSING(charArray);
                    }
                }

                Thread.Sleep(20);                
            }
            catch (Exception ex)
            {
                Global.EventLog(ex.Message);
            }                              
        }

        private static void _Led_Status_read()
        {
            try
            {
                // LED status
                string sData = string.Empty;
                sData += sCode;
                sData += sAdr;
                sData += "GET:LEST";
                sData += ";";
                string _chksumData = Global.Checksum(sData);

                string send_Command = string.Format("{0}{1}{2}{3}", Convert.ToChar(RS_SOH), sData, _chksumData, Convert.ToChar(RS_CR));
                _serialPort.Write(send_Command);
                Global.EventLog("Send : " + send_Command);

                Thread.Sleep(20);

                string readData = _serialPort.ReadTo(Convert.ToChar(RS_CR).ToString());
                Global.EventLog("Recv : " + readData);
                _serialPort.DiscardInBuffer();

                if (_DataCheck(readData) == 0)
                {
                    int nSize = readData.Length;
                    int bufPos = 0;
                    char[] charArray;
                    charArray = new char[nSize];
                    for (int i = 0; i < nSize; i++)
                    {
                        charArray[bufPos] = readData[i];
                        bufPos++;
                    }

                    if ((charArray[13] == Convert.ToChar(0x2F)) && (charArray[21] == Convert.ToChar(0x3B)))
                    {
                        _LED_STATUS_PARSING(charArray);
                    }
                }

                Thread.Sleep(20);
            }
            catch (Exception ex)
            {
                Global.EventLog(ex.Message);
            }            
        }

        private static void _STATUS_PARSING(char[] cArrData)
        {
            // 에러 상태
            if      (cArrData[14] == '0')   loadPortStatus.sR_ErrorSts = "Normal";
            else if (cArrData[14] == 'A')   loadPortStatus.sR_ErrorSts = "RecoverableError";
            else if (cArrData[14] == 'E')   loadPortStatus.sR_ErrorSts = "UnrecoverableError";

            // 모드
            if      (cArrData[15] == '0')   loadPortStatus.sR_Mode = "Online";
            else if (cArrData[15] == '1')   loadPortStatus.sR_Mode = "Teaching";
            else if (cArrData[15] == '2')   loadPortStatus.sR_Mode = "Maintenance";

            // 장치 상태
            if      (cArrData[16] == '0')   loadPortStatus.sR_DeviceSts = "InOperation";
            else if (cArrData[16] == '1')   loadPortStatus.sR_DeviceSts = "Home";
            else if (cArrData[16] == '2')   loadPortStatus.sR_DeviceSts = "LOAD";

            // 동작 상태
            if      (cArrData[17] == '0')   loadPortStatus.sR_OperationSts = "Stopping";
            else if (cArrData[17] == '1')   loadPortStatus.sR_OperationSts = "Operating";

            // 에러 코드(상위)
            loadPortStatus.sR_ErrorCode[0] = cArrData[18].ToString();
            // 에러 코드(하위)
            loadPortStatus.sR_ErrorCode[1] = cArrData[19].ToString();

            // 용기 상태
            if      (cArrData[20] == '0')   loadPortStatus.sR_ContainerSts = "None";
            else if (cArrData[20] == '1')   loadPortStatus.sR_ContainerSts = "Normal";
            else if (cArrData[20] == '2')   loadPortStatus.sR_ContainerSts = "Abnormal";

            // 클램프 위치
            if      (cArrData[21] == '0')   loadPortStatus.sR_ClampPosition = "Unclamp";
            else if (cArrData[21] == '1')   loadPortStatus.sR_ClampPosition = "Clamp";
            else if (cArrData[21] == '?')   loadPortStatus.sR_ClampPosition = "Indefinite";

            // 도어 래치 위치
            if      (cArrData[22] == '0')   loadPortStatus.sR_DoorLatchPosition = "Open";
            else if (cArrData[22] == '1')   loadPortStatus.sR_DoorLatchPosition = "Close";
            else if (cArrData[22] == '?')   loadPortStatus.sR_DoorLatchPosition = "Indefinite";

            // 흡착 상태
            if      (cArrData[23] == '0')   loadPortStatus.sR_AdsorptionSts = "Off";
            else if (cArrData[23] == '1')   loadPortStatus.sR_AdsorptionSts = "On";

            // 도어 위치
            if      (cArrData[24] == '0')   loadPortStatus.sR_DoorPosition = "Open";
            else if (cArrData[24] == '1')   loadPortStatus.sR_DoorPosition = "Close";
            else if (cArrData[24] == '?')   loadPortStatus.sR_DoorPosition = "Indefinite";

            // 웨이퍼 돌출 센서
            if      (cArrData[25] == '0')   loadPortStatus.sR_WaferProtrusionSns = "Shading";
            else if (cArrData[25] == '1')   loadPortStatus.sR_WaferProtrusionSns = "Lighting";

            // 승강축 위치
            if      (cArrData[26] == '0')   loadPortStatus.sR_ElevatorAxisPosition = "Rising";
            else if (cArrData[26] == '1')   loadPortStatus.sR_ElevatorAxisPosition = "Lowering";
            else if (cArrData[26] == '2')   loadPortStatus.sR_ElevatorAxisPosition = "MappStart";
            else if (cArrData[26] == '3')   loadPortStatus.sR_ElevatorAxisPosition = "MappEnd";
            else if (cArrData[26] == '?')   loadPortStatus.sR_ElevatorAxisPosition = "Indefinite";

            // 독 위치
            if      (cArrData[27] == '0')   loadPortStatus.sR_DockPosition = "Undock";
            else if (cArrData[27] == '1')   loadPortStatus.sR_DockPosition = "Dock";
            else if (cArrData[27] == '?')   loadPortStatus.sR_DockPosition = "Indefinite";

            // 맵핑 대기 위치
            if      (cArrData[29] == '0')   loadPortStatus.sR_MappWaitPosition = "Waiting";
            else if (cArrData[29] == '1')   loadPortStatus.sR_MappWaitPosition = "Measuring";
            else if (cArrData[29] == '?')   loadPortStatus.sR_MappWaitPosition = "Indefinite";

            // 맵핑 상태
            if      (cArrData[31] == '0')   loadPortStatus.sR_MappSts = "Inexecution";
            else if (cArrData[31] == '1')   loadPortStatus.sR_MappSts = "NormalEnd";
            else if (cArrData[31] == '2')   loadPortStatus.sR_MappSts = "AbnormalEnd";

            // 기종
            if      (cArrData[32] == '0')   loadPortStatus.sR_Type = "TYPE-1";
            else if (cArrData[32] == '1')   loadPortStatus.sR_Type = "TYPE-2";
            else if (cArrData[32] == '2')   loadPortStatus.sR_Type = "TYPE-3";
            else if (cArrData[32] == '3')   loadPortStatus.sR_Type = "TYPE-4";
            else if (cArrData[32] == '4')   loadPortStatus.sR_Type = "TYPE-5";


            // 에러 코드 Parsing
            string sE1 = loadPortStatus.sR_ErrorCode[0];
            string sE2 = loadPortStatus.sR_ErrorCode[1];
            error_List.error_code = string.Format("{0}{1}", sE1, sE2);
            sErrorCodeName = error_List.error_code;
            if (error_List.error_code != "00")
            {
                Global.EventLog("Error Code : " + sErrorCodeName);
            }
        }

        private static void _LED_STATUS_PARSING(char[] cArrData)
        {
            // PRESENCE LED의 상태
            if      (cArrData[14] == '0') ledStatus.sR_Presence = "Off";
            else if (cArrData[14] == '1') ledStatus.sR_Presence = "On";
            else if (cArrData[14] == '2') ledStatus.sR_Presence = "Blink";

            // PLACEMENT LED의 상태
            if      (cArrData[15] == '0') ledStatus.sR_Placement = "Off";
            else if (cArrData[15] == '1') ledStatus.sR_Placement = "On";
            else if (cArrData[15] == '2') ledStatus.sR_Placement = "Blink";

            // LOAD LED의 상태
            if      (cArrData[16] == '0') ledStatus.sR_Load = "Off";
            else if (cArrData[16] == '1') ledStatus.sR_Load = "On";
            else if (cArrData[16] == '2') ledStatus.sR_Load = "Blink";

            // UNLOAD LED의 상태
            if      (cArrData[17] == '0') ledStatus.sR_Unload = "Off";
            else if (cArrData[17] == '1') ledStatus.sR_Unload = "On";
            else if (cArrData[17] == '2') ledStatus.sR_Unload = "Blink";

            // OperatorAccess1의 상태
            if      (cArrData[18] == '0') ledStatus.sR_OpAccess1 = "Off";
            else if (cArrData[18] == '1') ledStatus.sR_OpAccess1 = "On";
            else if (cArrData[18] == '2') ledStatus.sR_OpAccess1 = "Blink";

            // STATUS1 LED의 상태
            if      (cArrData[19] == '0') ledStatus.sR_Status1 = "Off";
            else if (cArrData[19] == '1') ledStatus.sR_Status1 = "On";
            else if (cArrData[19] == '2') ledStatus.sR_Status1 = "Blink";

            // STATUS2 LED의 상태
            if      (cArrData[20] == '0') ledStatus.sR_Status2 = "Off";
            else if (cArrData[20] == '1') ledStatus.sR_Status2 = "On";
            else if (cArrData[20] == '2') ledStatus.sR_Status2 = "Blink";

            // OperatorAccess2의 상태
            if      (cArrData[21] == '0') ledStatus.sR_OpAccess2 = "Off";
            else if (cArrData[21] == '1') ledStatus.sR_OpAccess2 = "On";
            else if (cArrData[21] == '2') ledStatus.sR_OpAccess2 = "Blink";
        }
        #endregion

        #region SET Command (설정 커맨드)
        // 에러 해제
        public static void ErrorClear()
        {
            _bSet_flag = true;            
                       
            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:RSET";
            sData += ";";
            string _chksumData = Global.Checksum(sData);            

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 동작 Retry
        // 동작 정지(계속 동작 불가)시 실행 가능        
        public static void Retry()
        {
            _bSet_flag = true;
            
            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:RTRY";
            sData += ";";
            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);            

            _bSet_flag = false;
        }

        // 동작 정지(계속 동작 불가)        
        public static void Stop()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:STPP";
            sData += ";";
            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }        

        // 동작 계속
        // 동작 정지(계속 동작 가능) 시 실행 가능
        public static void Resume()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:RESM";
            sData += ";";
            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 동작 정지(계속 동작 가능)
        public static void Pause()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:PASE";
            sData += ";";
            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 커맨드 어보트
        // 동작 정지 시 실행 가능(계속 동작, 동작 리트라이 불가)
        public static void Abort()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:ABOT";
            sData += ";";
            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // PRESENCE LED
        public static void LED_Presence(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPON"; // 점등
            else if (sCommand == "Off")   sData += "LOON"; // 소등
            else if (sCommand == "Blink") sData += "BLON"; // 점멸
            sData += ";";
            
            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // PLACEMENT LED
        public static void LED_Placement(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPST"; // 점등
            else if (sCommand == "Off")   sData += "LOST"; // 소등
            else if (sCommand == "Blink") sData += "BLST"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // LOAD LED
        public static void LED_Load(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPLD"; // 점등
            else if (sCommand == "Off")   sData += "LOLD"; // 소등
            else if (sCommand == "Blink") sData += "BLLD"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // UNLOAD LED
        public static void LED_Unload(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPUD"; // 점등
            else if (sCommand == "Off")   sData += "LOUD"; // 소등
            else if (sCommand == "Blink") sData += "BLUD"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // ALARM LED
        public static void LED_Alarm(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPAL"; // 점등
            else if (sCommand == "Off")   sData += "LOAL"; // 소등
            else if (sCommand == "Blink") sData += "BLAL"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // OPERATION ACCESS LED#1
        public static void LED_OpAccess1(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPSW"; // 점등
            else if (sCommand == "Off")   sData += "LOSW"; // 소등
            else if (sCommand == "Blink") sData += "BLSW"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // OPERATION ACCESS LED#2
        public static void LED_OpAccess2(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPSL"; // 점등
            else if (sCommand == "Off")   sData += "LOSL"; // 소등
            else if (sCommand == "Blink") sData += "BLSL"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // STATUS LED#1
        public static void LED_Status1(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPS1"; // 점등
            else if (sCommand == "Off")   sData += "LOS1"; // 소등
            else if (sCommand == "Blink") sData += "BLS1"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // STATUS LED#2
        public static void LED_Status2(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPS2"; // 점등
            else if (sCommand == "Off")   sData += "LOS2"; // 소등
            else if (sCommand == "Blink") sData += "BLS2"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // STATUS LED#3
        public static void LED_Status3(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPS3"; // 점등
            else if (sCommand == "Off")   sData += "LOS3"; // 소등
            else if (sCommand == "Blink") sData += "BLS3"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // STATUS LED#4
        public static void LED_Status4(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "SET:";
            if      (sCommand == "On")    sData += "LPS4"; // 점등
            else if (sCommand == "Off")   sData += "LOS4"; // 소등
            else if (sCommand == "Blink") sData += "BLS4"; // 점멸
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }
        #endregion

        #region MOV Command (동작 커맨드 (복합 동작))
        // 초기 위치로 이동
        public static void Origin()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:ORGN";            
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 초기 위치로 강제 이동
        public static void ForcedOrigin()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:ABGN";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 언로드 상태에서 로드 상태로 이동
        public static void UnloadToLoad()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FPLD";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 언로드 상태에서 맵핑 동작 후, 로드 상태로 이동
        public static void UnloadToMapp_Load()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FPML";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 언로드 상태에서 독 상태로 이동
        public static void UnloadToDock()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FDOC";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 독 상태에서 로드 상태로 이동
        public static void DockToLoad()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FDLD";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 독 상태에서 맵핑 동작 후, 로드 상태로 이동
        public static void DockToMapp_Load()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FDML";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 클램프 상태에서 로드 상태로 이동
        public static void ClampToLoad()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FCLD";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 클램프 상태에서 맵핑 동작 후, 로드 상태로 이동
        public static void ClampToMapp_Load()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FCML";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 로드 상태에서 언로드 상태로 이동
        public static void LoadToUnload()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FPUL";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 로드 상태에서 맵핑 동작 후, 언로드 상태로 이동
        public static void LoadToMapp_Unload()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FPMU";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 로드 상태에서 독 상태로 이동
        public static void LoadToDock()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FVOF";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 독 상태에서 언로드 상태로 이동
        public static void DockToUnload()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FVUL";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 로드 상태에서 클램프 상태로 이동
        public static void LoadToClamp()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FUDC";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 로드 상태에서 맵핑 동작 후, 클램프 상태로 이동
        public static void LoadToMapp_Clamp()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FUMD";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 맵핑 동작 수행(상단으로부터 하단)
        public static void Mapp_TopToBottom()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:MAPP";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 재 맵핑 동작(상단으로부터 하단)
        public static void ReMapp_TopToBottom()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:RMAP";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }
        #endregion

        #region MOV Command (동작 커맨드 (개별 동작))
        // 언클램프 동작
        public static void UnClamp()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FCOP";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 클램프 동작
        public static void Clamp()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:FCCL";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 흡착 ON, OFF 동작
        public static void Vacuum(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:";
            if (sCommand == "On") sData += "VCON";
            else if (sCommand == "Off") sData += "VCOF";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 도어 클램프 오픈(FOUP 도어 언록 상태)
        public static void DoorClampOpen()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:DROP";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 도어 클램프 클로즈(FOUP 도어 록 상태)
        public static void DoorClampClose()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:DRCL";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 맵핑 측정 위치로 이동
        public static void MoveToMapp()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:MAFW";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 맵핑 대기 위치로 이동
        public static void MoveToMappWait()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:MABW";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 승강축을 도어 개폐 위치로 이동
        public static void Elevator_MoveToDoor()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:Z_UP";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 승강축을 로드 위치로 이동
        public static void Elevator_MoveToLoad()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:Z_DN";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 승강축을 맵핑 개시 위치로 이동
        public static void Elevator_MoveToMapp()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:Z_ST";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 승강축을 맵핑 종료 위치로 이동
        public static void Elevator_MoveToMappEnd()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:Z_ED";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 독 슬라이드를 언독 위치로 이동
        public static void DockSlide_MoveToUndock()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:Y_BW";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // 독 슬라이드를 독 위치로 이동
        public static void DockSlide_MoveToDock()
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:Y_FW";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }

        // Door Open, Close 동작
        public static void Door(string sCommand)
        {
            _bSet_flag = true;

            string sData = string.Empty;
            sData += sCode;
            sData += sAdr;
            sData += "MOV:";
            if (sCommand == "Open") sData += "DRFW";
            else if (sCommand == "Close") sData += "DRBW";
            sData += ";";

            string _chksumData = Global.Checksum(sData);

            _SerialDataWrite(sData, _chksumData);

            _bSet_flag = false;
        }
        #endregion

        private static void _SerialDataWrite(string strData, string strChksumData)
        {
            try
            {
                string readData = string.Empty;
                string send_Command = string.Empty;
                send_Command = string.Format("{0}{1}{2}{3}", Convert.ToChar(RS_SOH), strData, strChksumData, Convert.ToChar(RS_CR));
                _serialPort.Write(send_Command);
                Global.EventLog("Send : " + send_Command);

                Thread.Sleep(20);

                readData = _serialPort.ReadTo(Convert.ToChar(RS_CR).ToString());
                Global.EventLog("Recv : " + readData);

                Thread.Sleep(20);
            }
            catch (Exception ex) 
            {
                Global.EventLog(ex.Message);
            }            
        }

        private static int _DataCheck(string sReadData)
        {
            try
            {
                if (sReadData.Length > 1)
                {
                    int nSize = sReadData.Length;
                    int bufPos = 0;
                    char[] charArray;
                    charArray = new char[nSize];
                    for (int i = 0; i < nSize; i++)
                    {
                        charArray[bufPos++] = sReadData[i];
                    }

                    if ((charArray[0] == Convert.ToChar(RS_SOH)) &&
                        (charArray[1] == '0') && (charArray[2] == '0') &&
                        (charArray[3] == '0') && (charArray[4] == '0'))
                    {
                        return 0;
                    }
                    else if ((charArray[1] == '0') && (charArray[2] == '4'))
                    {
                        int index = sReadData.IndexOf("/");
                        string strTmp = string.Format("{0}{1}", sReadData.Substring(index + 1, 1), sReadData.Substring(index + 2, 1));
                        interlock_List.interlock_code = strTmp;
                        sInterlockName = interlock_List.interlock_code;
                        Global.EventLog("Interlock Code : " + sInterlockName);

                        return -1;
                    }
                    else
                    {
                        string strTmp = string.Format("{0}{1}", charArray[1], charArray[2]);
                        response_List.response_code = strTmp;
                        sResponseName = response_List.response_code;
                        Global.EventLog("Response Code : " + sResponseName);

                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Global.EventLog(ex.Message);
                return -1;
            }                      
        }        
    }
}
