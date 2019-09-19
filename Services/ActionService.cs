﻿using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using newapi.Helpers;
using Newtonsoft.Json.Linq;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using wscore.Entities;
using wscore.Helpers;

namespace wscore.Services
{
    public interface IActionService
    {
        DepositReturn Deposit(int terminalId, string number, int total, int userId);
        ActionReturn DepositTCP(int terminalId, int userId);
        ActionReturn OpenDoor(int terminalId, int userId);
        ActionReturn OpenDoorTCP(EventTCP action);
        ActionReturn Reboot(int terminalId, int userId);
        ActionReturn RebootTCP(int terminalId, int userId);
        TerminalReturn Status(int terminalId);
        List<TerminalReturn> Terminals();
        DepositReturn GetDeposit(int DepositId, int userId);
        TerminalReturn UpdateDepositTimeOff(int terminalId, int timeOff, int userId);
        ActionReturn DepositCancel(int DepositId, int TerminalId, int userId);
        void UpdateTerminal(UpdateTerminalReturn updateTerminal);
        TotalAmount getAllTerminalsTotalAmount();
        List<TotalAmount> getAllTotalDeposit();
        List<TotalAmount> getAllTotalWithdraw();
        List<TerminalsList> getAllOfflineTerminals();
        TerminalsCapacity getTerminalsCapacity();
        List<TerminalsList> getAllTerminalsPercentage();
        List<DepositListReturn> getDeposits(ReportRequest depositParam);
        List<WithdrawListReturn> getWidthraws(ReportRequest withdrawParam);
        Notes DepositNotes(int? depositId);
        Notes WithdrawNotes(int? eventId);
        List<TerminalIdsReturn> GetTerminalsIds();
        TerminalCapacityBills GetTerminalCapacityBills(int? TerminalId);
        List<DailyDepositReturn> getDepositsByTerminal(int? TerminalId);
        List<TotalTerminalBills> getTotalTerminalBills(int? TerminalId);
        CashBoxNotes GetCashBoxBills(CashBoxRequest cashBoxparam);
        List<EventListReturn> getEventsByTerminal(EventRequest eventRequest);
        List<EventListReturn> getEventsByTerminal(int? TerminalId);
        void asignUserToTerminal(TerminalUserRequest requestParam);
        List<UserReturn> getAllTerminalUsers(int? terminalId);
        void unasignUserFromTerminal(TerminalUserRequest requestParam);
        TerminalStatusReturn isTerminalOnline(int? TerminalId);

    }

    public class ActionService : IActionService
    {

        public ActionService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;

        }

        #region Private

        private readonly AppSettings _appSettings;
        private string calendarDate = "0000";

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_appSettings.DefaultConnection);
        }

        #region Terminal

        private List<Terminal> ListTerminal()
        {
            List<Terminal> _listTerminal = new List<Terminal>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
               // MySqlCommand cmd = new MySqlCommand("select * from Terminal ", conn);
                MySqlCommand cmd = new MySqlCommand("select Name, Address, TerminalId, TerminalDoor, LastComunication, ((currentNotes * 100) / totalCashBox) as percentageTerminal" +
                    " from(select T.Name, T.Address, T.TerminalId, T.TerminalDoor, T.LastComunication, sum(Notes1000 + Notes500 + Notes200 + Notes100 + Notes50 + Notes20 + Notes10 + Notes5 + Notes2 + Notes1)" +
                    " as currentNotes, sum(CashBoxCapacity) as totalCashBox from Terminal T left join TerminalNotes N on T.TerminalId = N.TerminalId group by T.TerminalId) as total; ", conn);
                    
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Terminal _terminal = new Terminal();
                        _terminal.Address = reader["Address"].ToString();
                      //  _terminal.IP = reader["IP"].ToString();
                      //  _terminal.Description = reader["Description"].ToString();
                      //  _terminal.TimeOff = int.Parse(reader["timeOff"].ToString());
                        _terminal.TerminalDoor = reader["TerminalDoor"].ToString();
                        _terminal.Name = reader["Name"].ToString();
                      //  _terminal.CashBoxDoor = reader["CashBoxDoor"].ToString();
                     //   _terminal.Notes = int.Parse(reader["Notes"].ToString());
                        _terminal.TerminalId = int.Parse(reader["TerminalId"].ToString());
                     //   _terminal.TotalAmount = int.Parse(reader["TotalAmount"].ToString());
                        _terminal.LastComunication = reader["LastComunication"] != DBNull.Value ? DateTime.Parse(reader["LastComunication"].ToString()) : DateTime.Parse("1990-01-01 00:00:00")  ;
                        _terminal.percentageTerminal = reader["percentageTerminal"] != DBNull.Value ? double.Parse(reader["percentageTerminal"].ToString()) : 0;
                        _listTerminal.Add(_terminal);
                    }
                   
                }
            }

            return _listTerminal;
        }

        private Terminal GetTerminal(int terminalId)
        {
            Terminal _terminal = null; ;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select * from Terminal where TerminalId=" + terminalId.ToString(), conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _terminal = new Terminal();
                        _terminal.Address = reader["Address"].ToString();
                        _terminal.IP = reader["IP"].ToString();
                        _terminal.Description = reader["Description"].ToString();
                        _terminal.TimeOff = int.Parse(reader["timeOff"].ToString());
                        _terminal.TerminalDoor = reader["TerminalDoor"].ToString();
                        _terminal.Name = reader["Name"].ToString();
                        _terminal.CashBoxDoor = reader["CashBoxDoor"].ToString();
                        _terminal.Notes = int.Parse(reader["Notes"].ToString());
                        _terminal.TerminalId = int.Parse(reader["TerminalId"].ToString());
                        _terminal.TotalAmount = int.Parse(reader["TotalAmount"].ToString());
                        _terminal.ContactName = reader["ContactName"].ToString();
                        _terminal.ContactPhone = reader["ContactPhone"].ToString();
                    }
                }
            }

            return _terminal;
        }

        private Terminal GetTerminalByName(string name)
        {
            Terminal _terminal = null; ;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select * from Terminal where Name= '" + name.ToString() + "' ", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _terminal = new Terminal();
                        _terminal.Name = reader["Name"].ToString();
                        _terminal.TerminalId = int.Parse(reader["TerminalId"].ToString());
                    }
                }
            }
            return _terminal;
        }

        public bool isNameUnique(string name)
        {
            var isUnique = GetTerminalByName(name);

            if (isUnique == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Notes GetDepositNotes(int depositId)
        {
            Notes _notes = null; ;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select * from DepositNotes where DepositId=" + depositId.ToString(), conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _notes = new Notes();
                        _notes.Note1 = int.Parse(reader["Notes1"].ToString());
                        _notes.Note2 = int.Parse(reader["Notes2"].ToString());
                        _notes.Note5 = int.Parse(reader["Notes5"].ToString());
                        _notes.Note10 = int.Parse(reader["Notes10"].ToString());
                        _notes.Note20 = int.Parse(reader["Notes20"].ToString());
                        _notes.Note50 = int.Parse(reader["Notes50"].ToString());
                        _notes.Note100 = int.Parse(reader["Notes100"].ToString());
                        _notes.Note200 = int.Parse(reader["Notes200"].ToString());
                        _notes.Note500 = int.Parse(reader["Notes500"].ToString());
                        _notes.Note1000 = int.Parse(reader["Notes1000"].ToString());
                    }
                }
            }
            return _notes;
        }

        private Notes GetWithdrawNotes(int eventId)
        {
            Notes _notes = null; ;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select EventId, Notes1000, Notes500, Notes200, Notes100, Notes50, Notes20, Notes10, Notes5, Notes2 , Notes1 from Withdraw where EventId = " + eventId.ToString(), conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _notes = new Notes();
                        _notes.Note1 = int.Parse(reader["Notes1"].ToString());
                        _notes.Note2 = int.Parse(reader["Notes2"].ToString());
                        _notes.Note5 = int.Parse(reader["Notes5"].ToString());
                        _notes.Note10 = int.Parse(reader["Notes10"].ToString());
                        _notes.Note20 = int.Parse(reader["Notes20"].ToString());
                        _notes.Note50 = int.Parse(reader["Notes50"].ToString());
                        _notes.Note100 = int.Parse(reader["Notes100"].ToString());
                        _notes.Note200 = int.Parse(reader["Notes200"].ToString());
                        _notes.Note500 = int.Parse(reader["Notes500"].ToString());
                        _notes.Note1000 = int.Parse(reader["Notes1000"].ToString());
                    }
                }
            }
            return _notes;
        }

        private TerminalCapacityBills GetTerminalCapacityBills(int terminalId)
        {
            TerminalCapacityBills billsCapacity = null;
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select TerminalId, totalCashBox, ((currentNotes * 100)/totalCashBox) as cashboxpercentage from(select TerminalId, " +
                    "sum(CashBoxCapacity) as totalCashBox, sum(Notes1000 + Notes500 + Notes200 + Notes100 + Notes50 + Notes20 + Notes10 + Notes5 + Notes2 + Notes1) as currentNotes" +
                    " from TerminalNotes group by TerminalId) as total where TerminalId=" + terminalId.ToString(), conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        billsCapacity = new TerminalCapacityBills();
                        billsCapacity.TerminalId = int.Parse(reader["TerminalId"].ToString());
                        billsCapacity.totalCashBox = int.Parse(reader["totalCashBox"].ToString());
                        billsCapacity.cashBoxPercentage = double.Parse(reader["cashboxpercentage"].ToString());
                    }
                }
            }
            return billsCapacity;
        }

        private List<TotalTerminalBills> GetTerminalTotalBills(int terminalId)
        {
            List<TotalTerminalBills> totalTerminalBills = new List<TotalTerminalBills>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select TerminalId, CashBoxCapacity, CashBoxNumber, CurrentQtyCashbox, totalAmount from(select TerminalId, CashBoxCapacity, CashBoxNumber," +
                    " sum(Notes1000 + Notes500 + Notes200 + Notes100 + Notes50 + Notes20 + Notes10 + Notes5 + Notes2 + Notes1) as CurrentQtyCashbox," +
                    " ((sum(Notes1000)) * 1000 + (sum(Notes500)) * 500 + (sum(Notes200)) * 200 + (sum(Notes100)) * 100 + (sum(Notes50)) * 50 + " +
                    "(sum(Notes20)) * 20 + (sum(Notes10)) * 10 + (sum(Notes5)) * 5 + (sum(Notes2)) * 2 + (sum(Notes1)) * 1) as totalAmount " +
                    "from TerminalNotes group by TerminalId, CashBoxCapacity, CashBoxNumber) as cash where TerminalId=" + terminalId.ToString(), conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TotalTerminalBills totalBills = new TotalTerminalBills();
                        totalBills.TerminalId = int.Parse(reader["TerminalId"].ToString());
                        totalBills.CashBoxCapacity = int.Parse(reader["CashBoxCapacity"].ToString());
                        totalBills.CashBoxNumber = int.Parse(reader["CashBoxNumber"].ToString());
                        totalBills.CurrentQtyCashbox = int.Parse(reader["CurrentQtyCashbox"].ToString());
                        totalBills.TotalAmount = int.Parse(reader["totalAmount"].ToString());
                        totalTerminalBills.Add(totalBills);
                    }
                }
            }
            return totalTerminalBills;
        }
        private CashBoxNotes GetCashBoxNumberBills(int? TerminalId, int? CashBoxNumber)
        {
            CashBoxNotes cashBoxBills = null;
            using (MySqlConnection conn = GetConnection())
            {
                // MySqlCommand cmd = new MySqlCommand("select CashBoxNumber, Notes1000, Notes500 , Notes200 , Notes100, Notes50, Notes20, Notes10, Notes5, Notes2, Notes1 " +
                //    "from TerminalNotes where TerminalId=" + TerminalId.ToString(), conn);

                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select CashBoxNumber, Notes1000, Notes500 , Notes200 , Notes100, Notes50, Notes20, Notes10, Notes5, Notes2, Notes1 " +
                    "from TerminalNotes where TerminalId= '" + TerminalId.ToString() + "' and CashBoxNumber=" + CashBoxNumber.ToString(), conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cashBoxBills = new CashBoxNotes();
                        cashBoxBills.CashBoxNumber = int.Parse(reader["CashBoxNumber"].ToString());
                        cashBoxBills.Note1 = int.Parse(reader["Notes1"].ToString());
                        cashBoxBills.Note2 = int.Parse(reader["Notes2"].ToString());
                        cashBoxBills.Note5 = int.Parse(reader["Notes5"].ToString());
                        cashBoxBills.Note10 = int.Parse(reader["Notes10"].ToString());
                        cashBoxBills.Note20 = int.Parse(reader["Notes20"].ToString());
                        cashBoxBills.Note50 = int.Parse(reader["Notes50"].ToString());
                        cashBoxBills.Note100 = int.Parse(reader["Notes100"].ToString());
                        cashBoxBills.Note200 = int.Parse(reader["Notes200"].ToString());
                        cashBoxBills.Note500 = int.Parse(reader["Notes500"].ToString());
                        cashBoxBills.Note1000 = int.Parse(reader["Notes1000"].ToString());

                    }
                }
            }
            return cashBoxBills;
        }

        private Notes GetTerminalNotes(int terminalId)
        {
            Notes _notes = null; ;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select * from TerminalNotes where TerminalId=" + terminalId.ToString(), conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _notes = new Notes();
                        _notes.Note1 = int.Parse(reader["Notes1"].ToString());
                        _notes.Note2 = int.Parse(reader["Notes2"].ToString());
                        _notes.Note5 = int.Parse(reader["Notes5"].ToString());
                        _notes.Note10 = int.Parse(reader["Notes10"].ToString());
                        _notes.Note20 = int.Parse(reader["Notes20"].ToString());
                        _notes.Note50 = int.Parse(reader["Notes50"].ToString());
                        _notes.Note100 = int.Parse(reader["Notes100"].ToString());
                        _notes.Note200 = int.Parse(reader["Notes200"].ToString());
                        _notes.Note500 = int.Parse(reader["Notes500"].ToString());
                        _notes.Note1000 = int.Parse(reader["Notes1000"].ToString());
                    }
                }
            }

            return _notes;
        }

        private Terminal UpdateTerminalTimeOff(Terminal terminal)
        {
            var _terminal = terminal;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("UPDATE Terminal SET timeoff = " + _terminal.TimeOff.ToString() + " WHERE TerminalId = " + _terminal.TerminalId.ToString() + ";", conn);
                cmd.ExecuteNonQuery();
            }

            return _terminal;
        }

        private bool IsOnline(string ip)
        {
            bool ok = true;

            Ping myPing = new Ping();
            PingReply reply = myPing.Send(ip, 1000);
            if (reply.Status == IPStatus.TimedOut)
            {
                ok = false;
            }

            return ok;
        }

        #endregion

        #region Event

        private Event EventInsert(Event Event)
        {
            var _event = Event;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("insert into Event (TerminalId,EventTypeId,UserId,Date) values (" + _event.TerminalId.ToString() + "," + _event.EventTypeId.ToString() + "," + _event.UserId.ToString() + ", NOW());SELECT LAST_INSERT_ID();", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _event.EventId = Convert.ToInt32(reader[0]);
                    }
                }
            }

            return _event;
        }

        private Event EventUpdate(Event Event)
        {
            var _event = Event;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("UPDATE Event SET Status = '" + _event.Status.ToString() + "' WHERE EventId = " + _event.EventId.ToString() + ";", conn);
                cmd.ExecuteNonQuery();
            }

            return _event;
        }

        #region Event From Terminal   

        private void EventTerminalInsert(Event Event)
        {
            var _event = Event;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("insert into EventTerminal (TerminalId,EventTypeId,Date) values (" + _event.TerminalId.ToString() + "," + _event.EventTypeId.ToString() + ",'" + _event.Date + "');SELECT LAST_INSERT_ID();", conn);
                cmd.ExecuteNonQuery();
            }

        }

        #endregion

        #endregion

        #region Deposit

        private Deposit DepositInsert(Deposit deposit)
        {
            var _deposit = deposit;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("insert into Deposit (EventId,DepositNumber,Status,Amount) values (" + _deposit.EventId.ToString() + ",'" + _deposit.DepositNumber.ToString() + "','" + (int)_deposit.Status + "'," + _deposit.Amount + ");SELECT LAST_INSERT_ID();", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _deposit.DepositId = Convert.ToInt32(reader[0]);
                    }
                }
            }

            return _deposit;
        }

        private Deposit DepositCancel(Deposit deposit)
        {
            var _deposit = deposit;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("UPDATE Deposit SET Status = '" + (int)_deposit.Status + "', UserCancel = 1, DateEnd = NOW() WHERE DepositId = " + _deposit.DepositId.ToString() + ";", conn);
                cmd.ExecuteNonQuery();
            }

            return _deposit;
        }

        private Deposit DepositUpdate(Deposit deposit)
        {
            var _deposit = deposit;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("UPDATE Deposit SET Status = '" + (int)_deposit.Status + "', DateEnd = NOW() WHERE DepositId = " + _deposit.DepositId.ToString() + ";", conn);
                cmd.ExecuteNonQuery();
            }

            return _deposit;
        }

        private Deposit GetDeposit(int DepositId)
        {
            Deposit _deposit = null;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select d.*, u.UserName, e.TerminalId, u.UserId from Deposit d inner join Event e on d.EventId = e.EventId inner join User u on e.UserId = u.UserId where d.DepositId = " + DepositId.ToString(), conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _deposit = new Deposit();
                        _deposit.DepositId = int.Parse(reader["DepositId"].ToString());
                        _deposit.DepositNumber = reader["DepositNumber"].ToString();
                        _deposit.Status = (DepositStatus)Enum.Parse(typeof(DepositStatus), reader["Status"].ToString());//Enum.Parse(Type.GetType("DepositStatus"), reader["Status"].ToString());
                        _deposit.Amount = int.Parse(reader["Amount"].ToString());
                        _deposit.Date = reader["DateEnd"].ToString();
                        _deposit.UserId = int.Parse(reader["UserId"].ToString());
                        _deposit.UserName = reader["UserName"].ToString();
                        _deposit.TerminalId = int.Parse(reader["TerminalId"].ToString());
                    }
                }
            }

            return _deposit;
        }


        private Deposit GetBills(Deposit Deposit)
        {

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select * from DepositBill where DepositId=" + Deposit.DepositId.ToString(), conn);
                var myList = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        myList.Add(reader["Bill"].ToString());
                    }
                }
                Deposit.BillsDetail = myList.ToArray();
            }

            return Deposit;
        }

        private void DepositBillInsert(Deposit deposit)
        {
            var _deposit = deposit;

            string bills = null;
            int i = 0;
            foreach (string bill in _deposit.BillsDetail)
            {
                if (i == 0)
                {
                    bills = "(" + _deposit.DepositId.ToString() + "," + bill + ")";
                    i = 1;
                }
                else
                {
                    bills += ",(" + _deposit.DepositId.ToString() + "," + bill + ")";
                }
            }
            bills += ";";
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("insert into DepositBill (DepositId,Bill) values " + bills, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _deposit.DepositId = Convert.ToInt32(reader[0]);
                    }
                }
            }
        }

        #endregion

        #endregion

        public ActionReturn Reboot(int terminalId, int userId)
        {
            var _terminal = GetTerminal(terminalId);

            var _event = new Event();

            _event.TerminalId = terminalId;

            if (_terminal != null)
            {

                _event.EventTypeId = 13;
                _event.EventType = EventType.Reboot;
                _event.UserId = userId;
                _event = EventInsert(_event);

                if (IsOnline(_terminal.IP))
                {

                    string _url = "http://" + _terminal.IP + "/reboot.php";

                    try
                    {
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            var response = client.GetStringAsync(_url).Result;

                        }

                        _event.Status = EventStatus.Successful;
                        _event = EventUpdate(_event);

                    }
                    catch
                    {
                        _event.Status = EventStatus.Error;
                        _event = EventUpdate(_event);

                    }
                }
                else
                {
                    _event.Status = EventStatus.OffLine;
                    _event = EventUpdate(_event);
                }
            }
            else
            {
                _event.Status = EventStatus.Error;
            }

            var _eventReturn = new ActionReturn();
            _eventReturn.TerminalId = _event.TerminalId;
            _eventReturn.EventType = _event.EventType.ToString();
            _eventReturn.Status = _event.Status.ToString();

            return _eventReturn;
        }

        public ActionReturn RebootTCP(int terminalId, int userId)
        {
            var _terminal = GetTerminal(terminalId);

            var _event = new Event();

            _event.TerminalId = terminalId;

            if (_terminal != null)
            {

                _event.EventTypeId = 13;
                _event.EventType = EventType.Reboot;
                _event.UserId = userId;
                _event = EventInsert(_event);

                if (IsOnline(_terminal.IP))
                {

                    try
                    {

                        SimpleTcpClient clienttcp;
                        clienttcp = new SimpleTcpClient();
                        clienttcp.StringEncoder = Encoding.UTF8;
                        clienttcp.Connect(_terminal.IP, Convert.ToInt32("8910"));
                        clienttcp.WriteLineAndGetReply("reset", TimeSpan.FromSeconds(1));

                        _event.Status = EventStatus.Successful;
                        _event = EventUpdate(_event);
                    }
                    catch
                    {
                        _event.Status = EventStatus.Busy;
                        _event = EventUpdate(_event);
                    }
                }
                else
                {
                    _event.Status = EventStatus.OffLine;
                    _event = EventUpdate(_event);
                }
            }
            else
            {
                _event.Status = EventStatus.Error;
            }

            var _eventReturn = new ActionReturn();
            _eventReturn.TerminalId = _event.TerminalId;
            _eventReturn.EventType = _event.EventType.ToString();
            _eventReturn.Status = _event.Status.ToString();

            return _eventReturn;
        }

        public ActionReturn OpenDoorTCP(EventTCP action)
        {
            var _terminal = GetTerminal(action.TerminalId);

            var _eventReturn = new ActionReturn();
            _eventReturn.TerminalId = action.TerminalId;//_event.TerminalId;
            _eventReturn.EventType = EventType.OpenDoor.ToString();//_event.EventType.ToString();

            //var _event = new Event();

            //_event.TerminalId = terminalId;

            if (_terminal != null)
            {
                //_event.EventTypeId = 3;
                //_event.EventType = EventType.OpenDoor;
                //_event.UserId = userId;
                //_event = EventInsert(_event);


                if (IsOnline(_terminal.IP))
                {
                    try
                    {

                        action.Event = EventType.OpenDoor.ToString();

                        string strParam = Newtonsoft.Json.JsonConvert.SerializeObject(action);// DeserializeObject<Deposit>(jsonDepo);

                        SimpleTcpClient clienttcp;
                        clienttcp = new SimpleTcpClient();
                        clienttcp.StringEncoder = Encoding.UTF8;
                        clienttcp.Connect(_terminal.IP, Convert.ToInt32("8910"));
                        clienttcp.WriteLineAndGetReply(strParam, TimeSpan.FromSeconds(1));

                        _eventReturn.Status = EventStatus.Successful.ToString();
                        //_event = EventUpdate(_event);
                    }
                    catch
                    {
                        _eventReturn.Status = EventStatus.Busy.ToString(); ;
                        //_event = EventUpdate(_event);
                    }
                }
                else
                {
                    _eventReturn.Status = EventStatus.OffLine.ToString(); ;
                    //_event = EventUpdate(_event);
                }
            }
            else
            {
                _eventReturn.Status = EventStatus.Error.ToString();
            }

            //_eventReturn.Status = _event.Status.ToString();

            return _eventReturn;
        }

        public ActionReturn OpenDoor(int terminalId, int userId)
        {
            var _terminal = GetTerminal(terminalId);

            var _event = new Event();

            _event.TerminalId = terminalId;

            if (_terminal != null)
            {
                _event.EventTypeId = 3;
                _event.EventType = EventType.OpenDoor;
                _event.UserId = userId;
                _event = EventInsert(_event);

                if (IsOnline(_terminal.IP))
                {

                    string _url = "http://" + _terminal.IP + "/open.php";

                    try
                    {
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            var response = client.GetStringAsync(_url).Result;

                        }

                        _event.Status = EventStatus.Successful;
                        _event = EventUpdate(_event);

                    }
                    catch
                    {
                        _event.Status = EventStatus.Error;
                        _event = EventUpdate(_event);

                    }
                }
                else
                {
                    _event.Status = EventStatus.OffLine;
                    _event = EventUpdate(_event);
                }
            }
            else
            {
                _event.Status = EventStatus.Error;
            }
            var _eventReturn = new ActionReturn();
            _eventReturn.TerminalId = _event.TerminalId;
            _eventReturn.EventType = _event.EventType.ToString();
            _eventReturn.Status = _event.Status.ToString();

            return _eventReturn;
        }

        public DepositReturn Deposit(int terminalId, string number, int total, int userId)
        {
            var _terminal = GetTerminal(terminalId);

            var _event = new Event();

            _event.TerminalId = terminalId;
            _event.EventTypeId = 1;
            _event.EventType = EventType.Deposit;
            _event.UserId = userId;
            _event = EventInsert(_event);

            var _deposit = new Deposit();
            _deposit.EventId = _event.EventId;
            _deposit.DepositNumber = number;
            _deposit.Amount = total;
            _deposit.TerminalId = terminalId;

            if (_terminal != null)
            {

                if (IsOnline(_terminal.IP))
                {

                    _deposit.Status = DepositStatus.Sending;
                    _deposit = DepositInsert(_deposit);

                    string _url = "http://" + _terminal.IP + "/BasicValidator/Deposit.php?depositid=" + _deposit.DepositId.ToString() + "&total=" + _deposit.Amount.ToString() + "&timeoff=" + _terminal.TimeOff.ToString();

                    try
                    {
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            var response = client.GetStringAsync(_url).Result;
                            if (response == DepositStatus.Processing.ToString())
                            {
                                _event.Status = EventStatus.Successful;
                                _event = EventUpdate(_event);
                                _deposit.Status = DepositStatus.Processing;
                                _deposit = DepositUpdate(_deposit);

                            }
                            else if (response == DepositStatus.Error.ToString())
                            {
                                _event.Status = EventStatus.Successful;
                                _event = EventUpdate(_event);
                                _deposit.Status = DepositStatus.Error;
                                _deposit = DepositUpdate(_deposit);

                            }
                            else if (response == DepositStatus.Busy.ToString())
                            {
                                _event.Status = EventStatus.Successful;
                                _event = EventUpdate(_event);
                                _deposit.Status = DepositStatus.Busy;
                                _deposit = DepositUpdate(_deposit);

                            }
                        }

                    }
                    catch
                    {
                        _event.Status = EventStatus.Error;
                        _event = EventUpdate(_event);
                        _deposit.Status = DepositStatus.Error;
                        _deposit = DepositUpdate(_deposit);

                    }
                }
                else
                {
                    _event.Status = EventStatus.OffLine;
                    _event = EventUpdate(_event);
                    _deposit.Status = DepositStatus.OffLine;
                    _deposit = DepositUpdate(_deposit);
                }

            }

            DepositReturn _depositReturn = new DepositReturn();
            _depositReturn.Amount = _deposit.Amount;
            _depositReturn.Date = DateTime.Now.ToString();
            _depositReturn.DepositId = _deposit.DepositId;
            _depositReturn.Number = _deposit.DepositNumber;
            _depositReturn.Status = _deposit.Status.ToString();
            _depositReturn.TerminalId = _deposit.TerminalId;

            return _depositReturn;
        }

        public ActionReturn DepositTCP(int terminalId, int userId)
        {
            var _terminal = GetTerminal(terminalId);

            //var _event = new Event();

            //_event.TerminalId = terminalId;
            //_event.EventTypeId = 1;
            //_event.EventType = EventType.Deposit;
            //_event.UserId = userId;
            //_event = EventInsert(_event);

            //var _deposit = new Deposit();
            //_deposit.EventId = _event.EventId;
            //_deposit.DepositNumber = number;
            //_deposit.Amount = 0;
            //_deposit.TerminalId = terminalId;

            var _eventReturn = new ActionReturn();
            _eventReturn.TerminalId = terminalId;//_event.TerminalId;
            _eventReturn.EventType = EventType.Deposit.ToString();//_event.EventType.ToString();

            if (_terminal != null)
            {

                if (IsOnline(_terminal.IP))
                {

                    try
                    {

                        SimpleTcpClient clienttcp;
                        clienttcp = new SimpleTcpClient();
                        clienttcp.StringEncoder = Encoding.UTF8;
                        clienttcp.Connect(_terminal.IP, Convert.ToInt32("8910"));

                        //_deposit.Status = DepositStatus.Sending;
                        ////_deposit = DepositInsert(_deposit);

                        //_event.Status = EventStatus.Successful;
                        //_event = EventUpdate(_event);

                        //var resp = 
                        clienttcp.WriteLineAndGetReply("deposit" , TimeSpan.FromSeconds(2));

                        _eventReturn.Status = EventStatus.Successful.ToString();

                        //if (resp.MessageString != null)
                        //{
                        //    if (resp.MessageString.Contains("processing"))
                        //    {
                        //        _deposit.Status = DepositStatus.Processing;
                        //        //_deposit = DepositUpdate(_deposit);
                        //    }
                        //    else
                        //    {
                        //        _deposit.Status = DepositStatus.Error;
                        //        //_deposit = DepositUpdate(_deposit);
                        //    }
                        //}
                    }
                    catch
                    {
                        _eventReturn.Status = EventStatus.Busy.ToString(); ;
                        //_event.Status = EventStatus.Busy;
                        //_event = EventUpdate(_event);
                    }
                }
                else
                {
                    _eventReturn.Status = EventStatus.OffLine.ToString(); ;
                    //_event.Status = EventStatus.OffLine;
                    //_event = EventUpdate(_event);
                    //_deposit.Status = DepositStatus.OffLine;
                    //_deposit = DepositUpdate(_deposit);
                }

            }
            else
            {
                _eventReturn.Status = EventStatus.Error.ToString();
            }

            //DepositReturn _depositReturn = new DepositReturn();
            //_depositReturn.Amount = _deposit.Amount;
            //_depositReturn.Date = DateTime.Now.ToString();
            //_depositReturn.DepositId = _deposit.DepositId;
            //// _depositReturn.Number = _deposit.DepositNumber;
            //_depositReturn.Status = _deposit.Status.ToString();
            //_depositReturn.TerminalId = _deposit.TerminalId;

            return _eventReturn;
        }

        public TerminalReturn UpdateDepositTimeOff(int terminalId, int timeOff, int userId)
        {
            TerminalReturn _statusReturn = new TerminalReturn();
            var _terminal = GetTerminal(terminalId);
            var _event = new Event();

            _event.TerminalId = terminalId;

            if (_terminal != null)
            {
                _event.EventTypeId = 24;
                _event.EventType = EventType.TimeOffUpdate;
                _event.UserId = userId;
                _event = EventInsert(_event);

                _terminal.TimeOff = timeOff;

                UpdateTerminalTimeOff(_terminal);

                _statusReturn.TerminalId = _terminal.TerminalId;
                _statusReturn.Name = _terminal.Name;
                _statusReturn.Address = _terminal.Address;
                _statusReturn.Bills = _terminal.Notes;
                _statusReturn.CashBoxDoor = _terminal.CashBoxDoor;
                _statusReturn.Description = _terminal.Description;
                if (IsOnline(_terminal.IP))
                    _statusReturn.Status = Entities.TerminalStatus.Online.ToString();
                else
                    _statusReturn.Status = Entities.TerminalStatus.Offline.ToString();
                _statusReturn.TerminalDoor = _terminal.TerminalDoor;
                _statusReturn.TimeOff = _terminal.TimeOff;

                _event.Status = EventStatus.Successful;
                _event = EventUpdate(_event);
            }
            else
            {
                _statusReturn.TerminalId = terminalId;
                _statusReturn.Status = TerminalStatus.Error.ToString();
            }

            return _statusReturn;
        }

        public List<TerminalReturn> Terminals()
        {
            var _terminals = ListTerminal();
            List<TerminalReturn> _listReturn = new List<TerminalReturn>();
            if (_terminals != null)
            {
                foreach (Terminal t in _terminals)
                {
                    TerminalReturn r = new TerminalReturn();
                    r.TerminalId = t.TerminalId;
                    r.Name = t.Name;
                    r.Address = t.Address;
                   // r.Bills = t.Notes;
                   // r.CashBoxDoor = t.CashBoxDoor;
                   // r.Description = t.Description;
                    /* if (IsOnline(t.IP))
                         r.Status = Entities.TerminalStatus.Online.ToString();
                     else
                         r.Status = Entities.TerminalStatus.Offline.ToString();*/
                    r.TerminalDoor = t.TerminalDoor;
                   // r.TimeOff = t.TimeOff;
                  //  r.TotalAmount = t.TotalAmount;
                    r.LastComunication = t.LastComunication;
                    r.percentageTerminal = t.percentageTerminal;

                    _listReturn.Add(r);
                }
            }
            return _listReturn;
        }

        public TerminalReturn Status(int terminalId)
        {
            var _terminal = GetTerminal(terminalId);
            TerminalReturn _statusReturn = new TerminalReturn();
            if (_terminal != null)
            {
                _statusReturn.TerminalId = _terminal.TerminalId;
                _statusReturn.Name = _terminal.Name;
                _statusReturn.Address = _terminal.Address;
                _statusReturn.Bills = _terminal.Notes;
                _statusReturn.CashBoxDoor = _terminal.CashBoxDoor;
                _statusReturn.Description = _terminal.Description;
                if (IsOnline(_terminal.IP))
                    _statusReturn.Status = Entities.TerminalStatus.Online.ToString();
                else
                    _statusReturn.Status = Entities.TerminalStatus.Offline.ToString();
                _statusReturn.TerminalDoor = _terminal.TerminalDoor;
                _statusReturn.TimeOff = _terminal.TimeOff;
                _statusReturn.TotalAmount = _terminal.TotalAmount;
                _statusReturn.Notes = GetTerminalNotes(_terminal.TerminalId);
                _statusReturn.ContactName = _terminal.ContactName;
                _statusReturn.ContactPhone = _terminal.ContactPhone;
            }
            //missing validation when terminal does not exist  shoudl trow and errror.
            else
            {
                // _statusReturn.TerminalId = terminalId;
                //  _statusReturn.Status = TerminalStatus.Offline.ToString();
                throw new AppExceptions("Terminal Note not found");
            }

            return _statusReturn;
        }

        public DepositReturn GetDeposit(int DepositId, int userId)
        {
            var _deposit = GetDeposit(DepositId);
            DepositReturn _depositReturn = new DepositReturn();

            if (_deposit != null)
            {
                _deposit = GetBills(_deposit);

                _depositReturn.Amount = _deposit.Amount;
                _depositReturn.Date = DateTime.Parse(_deposit.Date).ToLocalTime().ToString();
                _depositReturn.DepositId = _deposit.DepositId;
                _depositReturn.Number = _deposit.DepositNumber;
                _depositReturn.Status = _deposit.Status.ToString();
                _depositReturn.TerminalId = _deposit.TerminalId;
                _depositReturn.BillsDetail = _deposit.BillsDetail;
                _depositReturn.UserName = _deposit.UserName;
            }

            return _depositReturn;

        }

        public ActionReturn DepositCancel(int DepositId, int TerminalId, int userId)
        {
            var _deposit = GetDeposit(DepositId);
            DepositReturn _depositReturn = new DepositReturn();
            var _event = new Event();
            _event.TerminalId = TerminalId;
            _event.EventTypeId = 2;
            _event.EventType = EventType.DepositCancel;
            _event.UserId = userId;
            _event = EventInsert(_event);

            if (_deposit != null)
            {

                if ((_deposit.UserId == userId) & (_deposit.Status == DepositStatus.Processing) & (_deposit.TerminalId == TerminalId))
                {

                    var _terminal = GetTerminal(_deposit.TerminalId);

                    if (IsOnline(_terminal.IP))
                    {

                        string _url = "http://" + _terminal.IP + "/BasicValidator/DepositCancel.php";

                        try
                        {
                            using (var client = new HttpClient())
                            {
                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                var response = client.GetStringAsync(_url).Result;

                            }

                            _event.Status = EventStatus.Successful;
                            _event = EventUpdate(_event);


                        }
                        catch
                        {
                            _event.Status = EventStatus.Error;
                            _event = EventUpdate(_event);

                        }

                    }
                    else
                    {
                        _event.Status = EventStatus.OffLine;
                        _event = EventUpdate(_event);
                    }

                    /*_deposit = GetBills(_deposit);

                    _depositReturn.Amount = _deposit.Amount;
                    _depositReturn.Date = DateTime.Parse(_deposit.Date).ToLocalTime().ToString();
                    _depositReturn.DepositId = _deposit.DepositId;
                    _depositReturn.Number = _deposit.DepositNumber;
                    _depositReturn.Status = _deposit.Status.ToString();
                    _depositReturn.TerminalId = _deposit.TerminalId;
                    _depositReturn.BillsDetail = _deposit.BillsDetail;
                    _depositReturn.UserName = _deposit.UserName;*/

                }
                else
                {
                    _event.Status = EventStatus.Error;
                    _event = EventUpdate(_event);
                }
            }
            else
            {
                _event.Status = EventStatus.Error;
                _event = EventUpdate(_event);
            }


            var _eventReturn = new ActionReturn();
            _eventReturn.TerminalId = _event.TerminalId;
            _eventReturn.EventType = _event.EventType.ToString();
            _eventReturn.Status = _event.Status.ToString();

            return _eventReturn;

        }

        public void UpdateTerminal(UpdateTerminalReturn updateTerminal)
        {
            //check if terminal id exist , check if terminal name does not exist, check if values are empty
            if (updateTerminal == null)
            {
                throw new AppExceptions("Terminal Id is required");
            }
            if (string.IsNullOrEmpty(updateTerminal.Name))
            {
                throw new AppExceptions("Name is required");
            }

            else
            {
                var _terminal = GetTerminal(updateTerminal.TerminalId);

                var _updateTerminal = updateTerminal;

                if (_terminal == null)
                {
                    throw new AppExceptions("Terminal not found");
                }

                bool value = isNameUnique(updateTerminal.Name);

                if (updateTerminal.Name != _terminal.Name)
                {
                    if (!value)
                    {
                        throw new AppExceptions("Terminal name already exist");
                    }
                }

                UpdateSQl(_updateTerminal);
            }
        }

        private void UpdateSQl(UpdateTerminalReturn _updateTerminal)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("UPDATE Terminal SET Name = '" + _updateTerminal.Name + "' , Address = '" + _updateTerminal.Address + "' , Description= '" + _updateTerminal.Description + "'" +
                                                    ", ContactName='" + _updateTerminal.ContactName + "', ContactPhone='" + _updateTerminal.ContactPhone + "' " +
                                                    " WHERE TerminalId = " + _updateTerminal.TerminalId.ToString() + ";", conn);
                cmd.ExecuteNonQuery();
            }
        }

        public TotalAmount getAllTerminalsTotalAmount()
        {
            TotalAmount totalAmount = new TotalAmount();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                //MySqlCommand cmd = new MySqlCommand("select sum(TotalAmount) as TotalAmount, count(*) as terminalTotal from Terminal", conn);
                MySqlCommand cmd = new MySqlCommand("select count(Distinct terminalId) as terminalTotal, ((sum(Notes1000))*1000 + (sum(Notes500))*500 + (sum(Notes200))*200" +
                    " + (sum(Notes100))*100 + (sum(Notes50))*50 + (sum(Notes20))*20 + (sum(Notes10))*10 + (sum(Notes5))*5 + (sum(Notes2))*2 + (sum(Notes1))*1 ) as TotalAmount from TerminalNotes ", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        totalAmount.AllTotalAmount = double.Parse(reader["TotalAmount"].ToString());
                        totalAmount.totalTerminals = int.Parse(reader["terminalTotal"].ToString());
                    }
                }
            }
            return totalAmount;
        }

        public List<TotalAmount> getAllTotalDeposit()
        {
            List<TotalAmount> totalAmountList = new List<TotalAmount>();
            
            DateTime startnows = DateTime.Now.AddDays(-6);
            DateTime enddate = DateTime.Now.AddDays(+1);

            var _start = startnows.ToString("yyyy-MM-dd 00:00:00"); //2019-08-20 00:00:00
            var _end = enddate.ToString("yyyy-MM-dd 00:00:00");

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                //MySqlCommand cmd = new MySqlCommand("select sum(D.Amount) as TotalDeposit, convert(D.DateEnd, date)  as _date from Deposit D where D.DateEnd >= '" + startnow + "' and  D.DateEnd <= '"+ startnow.AddDays(+1) + "' group by convert(D.DateEnd, date) ", conn);
                MySqlCommand cmd = new MySqlCommand("select sum(D.Amount) as TotalDeposit, convert(D.DateEnd, date)  as _date, (SELECT COUNT(*) FROM Terminal) as totalterminal " +
                                                    "from Deposit D where D.DateEnd >= '" + _start + "' and  D.DateEnd <= '" + _end + "' group by convert(D.DateEnd, date) ", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TotalAmount total = new TotalAmount();
                        total.Totalamount = double.Parse(reader["TotalDeposit"].ToString());
                        total.totalTerminals = int.Parse(reader["totalterminal"].ToString());
                        total.Date = DateTime.Parse(reader["_date"].ToString());

                        totalAmountList.Add(total);
                    }
                }
            }
            return totalAmountList;
        }

        public List<TotalAmount> getAllTotalWithdraw()
        {
            List<TotalAmount> totalAmountList = new List<TotalAmount>();

            DateTime startnows = DateTime.Now.AddDays(-6);
            DateTime enddate = DateTime.Now.AddDays(+1);

            var _start = startnows.ToString("yyyy-MM-dd 00:00:00");
            var _end = enddate.ToString("yyyy-MM-dd 00:00:00");

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select ((sum(Notes1000))*1000 + (sum(Notes500))*500  + (sum(Notes200))*200 + (sum(Notes100))*100 + (sum(Notes50))*50 + (sum(Notes20))*20 +" +
                    " (sum(Notes10))*10 + (sum(Notes5))*5 + (sum(Notes2))*2 + (sum(Notes1))*1 ) as TotalWithdraw, convert(W.Date, date) as _date, (SELECT COUNT(*) FROM Terminal) as totalterminal " +
                    " from Withdraw W where W.Date >= '" + _start + "' and W.Date <= '" + _end + "' group by convert(W.Date, date); ", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TotalAmount total = new TotalAmount();
                        total.Totalamount = double.Parse(reader["TotalWithdraw"].ToString());
                        total.totalTerminals = int.Parse(reader["totalterminal"].ToString());
                        total.Date = DateTime.Parse(reader["_date"].ToString());

                        totalAmountList.Add(total);
                    }
                }
            }
            return totalAmountList;
        }


        public List<TerminalsList> getAllOfflineTerminals()
        {
            List<TerminalsList> offlineTerminals = new List<TerminalsList>();

            DateTime startnows1 = DateTime.Now;
            var _date = startnows1.ToString("yyyy-MM-dd HH':'mm':'ss");

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select Name, Address, LastComunication from Terminal where LastComunication <= date_sub('" + _date + "' , Interval 30 minute) ", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TerminalsList _offline = new TerminalsList();
                        _offline.Name = reader["Name"].ToString();
                        _offline.Address = reader["Address"].ToString();
                        _offline.LastComunication = DateTime.Parse(reader["LastComunication"].ToString());

                        offlineTerminals.Add(_offline);
                    }
                }
            }
            return offlineTerminals;
        }

        public TerminalsCapacity getTerminalsCapacity()
        {
            TerminalsCapacity terminalsCapacity = new TerminalsCapacity();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select totalSystemCapacity, currentNotes, ((currentNotes * 100) / totalSystemCapacity) as percentage " +
                                                    "from(select sum(Notes1000 + Notes500 + Notes200 + Notes100 + Notes50 + Notes20 + Notes10 + Notes5 + Notes2 + Notes1) as currentNotes," +
                                                    "(select sum(NotesCapacity) from Terminal) as totalSystemCapacity from TerminalNotes) as total", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        terminalsCapacity.TotalSystemCapacityNotes = int.Parse(reader["totalSystemCapacity"].ToString());
                        terminalsCapacity.currentNotes = int.Parse(reader["currentNotes"].ToString());
                        terminalsCapacity.percentageNotes = double.Parse(reader["percentage"].ToString());
                    }
                }
            }
            return terminalsCapacity;
        }

        public List<TerminalsList> getAllTerminalsPercentage()
        {
            List<TerminalsList> percetageTerminals = new List<TerminalsList>();
            int percentageTerminal = 75; //changes to 75 %

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select Name, Address, percentageTerminal from (select Name, Address, ((currentNotes * 100) / totalCashBox) as percentageTerminal " +
                                                    "from(select T.Name, T.Address, sum(Notes1000 + Notes500 + Notes200 + Notes100 + Notes50 + Notes20 + Notes10 + Notes5 + Notes2 + Notes1) as currentNotes," +
                                                    "sum(CashBoxCapacity) as totalCashBox from TerminalNotes N inner join Terminal T on T.TerminalId = N.TerminalId group by N.TerminalId) as total) as grandtotal" +
                                                    " where percentageTerminal >= '" + percentageTerminal + "'", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TerminalsList percentage = new TerminalsList();
                        percentage.Name = reader["Name"].ToString();
                        percentage.Address = reader["Address"].ToString();
                        percentage.percentageTerminal = double.Parse(reader["percentageTerminal"].ToString());

                        percetageTerminals.Add(percentage);
                    }
                }
            }
            return percetageTerminals;
        }
        //functions takes as parameters start, end date and terminal id
        public List<DepositListReturn> getDeposits(ReportRequest depositParam)
        {
            if (string.IsNullOrWhiteSpace(depositParam.StartDate.ToString()) || string.IsNullOrWhiteSpace(depositParam.EndDate.ToString()))
            {
                throw new AppExceptions("Date can not be empty");
            }
            string startDate = depositParam.StartDate.Value.ToString("yyyy-MM-dd 00:00:00");

            DateTime _endDate = DateTime.Parse(depositParam.EndDate.ToString());
            _endDate = _endDate.AddDays(+1);
            string endDate = _endDate.ToString("yyyy-MM-dd 00:00:00");

            var _deposits = ListDeposits(startDate, endDate, depositParam.TerminalId);

            return _deposits;
        }
        //function only takes as parameters terminalid no date is need it
        public List<DailyDepositReturn> getDepositsByTerminal(int? TerminalId)
        {
            List<DailyDepositReturn> _deposits = null;
            if (TerminalId != null)
            {

                DateTime end = DateTime.Now.AddDays(+1);
                string endDate = end.ToString("yyyy-MM-dd 00:00:00"); //today's date is the ending

               // var _endDate = "2019-01-26 00:00:00"; //needs to be changed

                //  DateTime _endDate = DateTime.Parse(depositParam.EndDate.ToString());
                DateTime _startDate = end.AddDays(-5);
                string startDate = _startDate.ToString("yyyy-MM-dd 00:00:00");

               // var _startdate = "2019-01-22 00:00:00"; //needs to be changed

              //  _deposits = ListDailyDeposits(_startdate, _endDate, TerminalId);
                _deposits = ListDailyDeposits(startDate, endDate, TerminalId);
            }
            else
            {
                throw new AppExceptions("Terminal Note not found");
            }
            return _deposits;
        }

        private List<DepositListReturn> ListDeposits(string startDate, string endDate, int? TerminalId)
        {
            List<DepositListReturn> _listDeposit = new List<DepositListReturn>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "";

                sql = "select t.Name, t.Address, Amount, DepositNumber, DepositId, DateEnd, u.FirstName , u.LastName from Deposit d inner join Terminal t " +
                    "on d.TerminalId = t.TerminalId inner join User u on d.UserId = u.UserId where d.DateEnd >= '" + startDate + "' and  d.DateEnd <= '" + endDate + "' ";

                if (TerminalId != null)
                {
                    sql += " and d.TerminalId = " + TerminalId + " ";
                }

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DepositListReturn _deposits = new DepositListReturn();
                        _deposits.TerminalName = reader["Name"].ToString();
                        _deposits.TerminalAddress = reader["Address"].ToString();
                        _deposits.Amount = int.Parse(reader["Amount"].ToString());
                        _deposits.DepositId = int.Parse(reader["DepositId"].ToString());
                        _deposits.DepositNumber = int.Parse(reader["DepositNumber"].ToString());
                        _deposits.Date = DateTime.Parse(reader["DateEnd"].ToString());
                        _deposits.UserNameDeposit = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                        _listDeposit.Add(_deposits);
                    }
                }
            }
            return _listDeposit;
        }

        private List<DailyDepositReturn> ListDailyDeposits(string startDate, string endDate, int? TerminalId)
        {
            List<DailyDepositReturn> _listDeposit = new List<DailyDepositReturn>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "";

                sql = "select TerminalId, sum(Amount) as TotalDeposit, convert(DateEnd, date) as _date from Deposit  where " +
                    "DateEnd >= '" + startDate + "' and DateEnd <= '" + endDate + "' and TerminalId = " + TerminalId + " group by TerminalId, convert(DateEnd, date) ";

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DailyDepositReturn _deposits = new DailyDepositReturn();
                        _deposits.TerminalId = int.Parse(reader["TerminalId"].ToString());
                        _deposits.TotalDeposit = int.Parse(reader["TotalDeposit"].ToString());
                        _deposits.Date = DateTime.Parse(reader["_date"].ToString());
                        _listDeposit.Add(_deposits);
                    }
                }
            }
            return _listDeposit;
        }

        public Notes DepositNotes(int? depositId)
        {
            Notes _depositnotes = null;
            if (depositId != null)
            {
                _depositnotes = GetDepositNotes(depositId.Value);
            }
            else
            {
                throw new AppExceptions("Deposit Note not found");
            }
            return _depositnotes;
        }

        public Notes WithdrawNotes(int? eventId)
        {
            Notes _withdrawnotes = null;
            if (eventId != null)
            {
                _withdrawnotes = GetWithdrawNotes(eventId.Value);
            }
            else
            {
                throw new AppExceptions("Withdraw Note not found");
            }
            return _withdrawnotes;
        }

        //function for the dropdowns for the reports, deposits and withdraw
        public List<TerminalIdsReturn> GetTerminalsIds()
        {
            List<TerminalIdsReturn> _listTerminalIds = new List<TerminalIdsReturn>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("select TerminalId, Name from Terminal ", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TerminalIdsReturn _terminal = new TerminalIdsReturn();
                        _terminal.TerminalId = int.Parse(reader["TerminalId"].ToString());
                        _terminal.Name = reader["Name"].ToString();
                        _listTerminalIds.Add(_terminal);
                    }
                }
            }
            return _listTerminalIds;
        }
        public TerminalCapacityBills GetTerminalCapacityBills(int? TerminalId)
        {
            TerminalCapacityBills billsCapacity = null;
            if (TerminalId != null)
            {
                billsCapacity = GetTerminalCapacityBills(TerminalId.Value);
            }
            else
            {
                throw new AppExceptions("Terminal not found");
            }
            return billsCapacity;
        }

        public List<TotalTerminalBills> getTotalTerminalBills(int? TerminalId)
        {
            List<TotalTerminalBills> totalbills = null;
            if (TerminalId != null)
            {
                totalbills = GetTerminalTotalBills(TerminalId.Value);
            }
            else
            {
                throw new AppExceptions("Terminal not found");
            }
            return totalbills;
        }

        public CashBoxNotes GetCashBoxBills(CashBoxRequest cashBoxparam)
        {
            CashBoxNotes cashBoxBills = null;
            if (cashBoxparam.TerminalId != null && cashBoxparam.CashBoxNumber != null)
            {
                cashBoxBills = GetCashBoxNumberBills(cashBoxparam.TerminalId, cashBoxparam.CashBoxNumber);
            }
            else
            {
                throw new AppExceptions("Terminal not found");
            }
            return cashBoxBills;
        }

        public List<EventListReturn> getEventsByTerminal(EventRequest eventRequest)
        {
            if (string.IsNullOrWhiteSpace(eventRequest.StartDate.ToString()) || string.IsNullOrWhiteSpace(eventRequest.EndDate.ToString()))
            {
                throw new AppExceptions("Date can not be empty");
            }
            if (eventRequest.Option == null)
            {
                throw new AppExceptions("Option can not be empty");
            }
            if (eventRequest.Option != 0 && eventRequest.Option != 1 && eventRequest.Option != 3 && eventRequest.Option != 9)
            {
                throw new AppExceptions("No data content available");
            }
            List<EventListReturn> _events = null;
            string startDate = "";
            string endDate = "";

            if (eventRequest.TerminalId != null)
            {
                startDate = eventRequest.StartDate.Value.ToString("yyyy-MM-dd 00:00:00");

                DateTime _endDate = DateTime.Parse(eventRequest.EndDate.ToString());
                _endDate = _endDate.AddDays(+1);
                endDate = _endDate.ToString("yyyy-MM-dd 00:00:00");

                _events = ListEvents(startDate, endDate, eventRequest.TerminalId, eventRequest.Option);
            }
            else
            {
                throw new AppExceptions("Terminal not found");
            }
            return _events;
        }
        public List<EventListReturn> getEventsByTerminal(int? TerminalId)
        {
            List<EventListReturn> _events = null;

            if (TerminalId != null)
            {
                _events = ListEvents(TerminalId);
            }
            else
            {
                throw new AppExceptions("Terminal not found");
            }
            return _events;
        }
        private List<EventListReturn> ListEvents(string startDate, string endDate, int? TerminalId, int? Option)
        {
            List<EventListReturn> _listEvents = new List<EventListReturn>();
            int OpenDoorOption = 3;
            int ClosedDoorOption = 9;
            int DepositOption = 1;
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "";

                sql = "select E.terminalId,D.DepositId ,Et.Description, E.EventId, Date, U.FirstName, U.LastName from Event E inner join EventType Et on E.EventTypeId = Et.EventId" +
                    " inner join User U on E.UserId = U.UserId left join Deposit D on E.EventId = D.EventId where E.TerminalId = '" + TerminalId + "' and E.Date >= '" + startDate + "' and E.Date <= '" + endDate + "' ";

                if (Option == 0)
                {
                    sql += " and (E.EventTypeId = ' " + OpenDoorOption + " ' or E.EventTypeId = ' " + ClosedDoorOption + " ' or E.EventTypeId = ' " + DepositOption + " ') ";
                }
                else if (Option == OpenDoorOption)
                {
                    sql += " and (E.EventTypeId = ' " + OpenDoorOption + " ' ) ";
                }
                else if (Option == ClosedDoorOption)
                {
                    sql += " and ( E.EventTypeId = ' " + ClosedDoorOption + " ' ) ";
                }
                else if (Option == DepositOption)
                {
                    sql += " and ( E.EventTypeId = ' " + DepositOption + " ') ";
                }
                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        EventListReturn _events = new EventListReturn();
                        _events.Date = DateTime.Parse(reader["Date"].ToString());
                        _events.Description = reader["Description"].ToString();
                        _events.UserNameEvent = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                        _events.DepositId = reader["DepositId"] != DBNull.Value ? int.Parse(reader["DepositId"].ToString()) : 0;
                        _listEvents.Add(_events);
                    }
                }
            }
            return _listEvents;
        }
        private List<EventListReturn> ListEvents(int? TerminalId)
        {
            List<EventListReturn> _listEvents = new List<EventListReturn>();
            int limit = 5;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "";

                sql = "select TerminalId, Date, U.FirstName, U.LastName, Et.Description from Event  E inner join EventType Et on E.EventTypeId = Et.EventId " +
                    "inner join User U on E.UserId = U.UserId where E.TerminalId = '" + TerminalId + "' order by Date desc LIMIT " + limit + " ";

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        EventListReturn _events = new EventListReturn();
                        _events.Date = DateTime.Parse(reader["Date"].ToString());
                        _events.Description = reader["Description"].ToString();
                        _events.UserNameEvent = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                        _listEvents.Add(_events);
                    }
                }
            }
            return _listEvents;
        }

        public void asignUserToTerminal(TerminalUserRequest requestParam)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "";

                sql = "INSERT INTO UserTerminal (UserId , TerminalId) value ((SELECT * FROM(SELECT " + requestParam.UserId + ") as temp1 " +
                    " WHERE EXISTS(SELECT * FROM User WHERE UserId = " + requestParam.UserId + ") LIMIT 1) , " +
                    "(SELECT * FROM(SELECT " + requestParam.TerminalId + ") as temp2  WHERE EXISTS(SELECT* FROM Terminal WHERE TerminalId = " + requestParam.TerminalId + ") LIMIT 1)) ";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062)
                    {
                        throw new AppExceptions("Duplicates can not be inserted");
                    }
                    else if (ex.Number == 1048)
                    {
                        throw new AppExceptions("Terminal or User does not exist");
                    }
                    else
                    {
                        throw new AppExceptions("Data could not be inserted");
                    }

                }
            }
        }

        public void unasignUserFromTerminal(TerminalUserRequest requestParam)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "";

                sql = "Delete from UserTerminal where UserId = " + requestParam.UserId + " and TerminalId = " + requestParam.TerminalId + " ";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new AppExceptions("User or Terminal not found");
                }
            }
        }

        public List<UserReturn> getAllTerminalUsers(int? terminalId)
        {
            List<UserReturn> _listUsers = new List<UserReturn>();
            if (terminalId != null)
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("select ut.UserId, u.UserName, u.FirstName , u.LastName, u.Code , c.Name from UserTerminal ut join User u on u.UserId = ut.UserId" +
                        " join UserGroup b on u.UserId = b.UserId join `Group` c on b.GroupId = c.GroupId where TerminalId = " + terminalId.ToString(), conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            UserReturn _user = new UserReturn();
                            _user.Id = int.Parse(reader["UserId"].ToString());
                            _user.Username = reader["Username"].ToString();
                            _user.FirstName = reader["FirstName"].ToString();
                            _user.LastName = reader["LastName"].ToString();
                            _user.Group = reader["Name"].ToString();
                            _user.accessCode = Int32.Parse(reader["Code"].ToString());

                            _listUsers.Add(_user);
                        }
                    }
                }
                return _listUsers;
            }
            else
            {
                throw new AppExceptions("Terminal not found");
            }
        }

        //functions takes as parameters start, end date and terminal id
        public List<WithdrawListReturn> getWidthraws(ReportRequest withdrawParam)
        {
            if (string.IsNullOrWhiteSpace(withdrawParam.StartDate.ToString()) || string.IsNullOrWhiteSpace(withdrawParam.EndDate.ToString()))
            {
                throw new AppExceptions("Date can not be empty");
            }
            string startDate = withdrawParam.StartDate.Value.ToString("yyyy-MM-dd 00:00:00");

            DateTime _endDate = DateTime.Parse(withdrawParam.EndDate.ToString());
            _endDate = _endDate.AddDays(+1);
            string endDate = _endDate.ToString("yyyy-MM-dd 00:00:00");

            var _withdraw = ListWithdraw(startDate, endDate, withdrawParam.TerminalId);

            return _withdraw;
        }

        private List<WithdrawListReturn> ListWithdraw(string startDate, string endDate, int? TerminalId)
        {
            List<WithdrawListReturn> _listWithdraw = new List<WithdrawListReturn>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "";

                sql = "select t.Name, t.Address, u.FirstName , u.LastName, Date , w.CashboxNumber , EventId,  " +
                    " ((Notes1000 * 1000) + (Notes500 * 500) + (Notes200 * 200) + (Notes100 * 100) + (Notes50 * 50) + (Notes20 * 20) + (Notes10 * 10) + (Notes5 * 5) + (Notes2 * 2) + (Notes1 * 1)) " +
                    "as totalWithdraw from Withdraw w inner join Terminal t on w.TerminalId = t.TerminalId inner join User u on w.UserId = u.UserId " +
                    " where w.Date >= '" + startDate + "' and w.Date <= '" + endDate + "' ";

                if (TerminalId != null)
                {
                    sql += " and w.TerminalId = " + TerminalId + " ";
                }

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        WithdrawListReturn _withdraw = new WithdrawListReturn();
                        _withdraw.TerminalName = reader["Name"].ToString();
                        _withdraw.TerminalAddress = reader["Address"].ToString();
                        _withdraw.Amount = int.Parse(reader["totalWithdraw"].ToString());
                        _withdraw.CashBoxNumber = int.Parse(reader["CashBoxNumber"].ToString());
                        _withdraw.Date = DateTime.Parse(reader["Date"].ToString());
                        _withdraw.UserNameWithdraw = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                        _withdraw.EventId = int.Parse(reader["EventId"].ToString());
                        _listWithdraw.Add(_withdraw);
                    }
                }
            }
            return _listWithdraw;
        }

        public TerminalStatusReturn isTerminalOnline(int? TerminalId)
        {
            TerminalStatusReturn terminalstatus = new TerminalStatusReturn();
              
            if (TerminalId != null)
            {
                Terminal _terminal = GetTerminal(TerminalId.Value);

                var status = IsOnline(_terminal.IP);

                if (status)
                {
                    terminalstatus.Status = "Online";
                }
                else
                {
                    terminalstatus.Status = "Offline";
                }
            }
            else
            {
                throw new AppExceptions("Terminal not found");
            }
            return terminalstatus;
        }



    }
}
