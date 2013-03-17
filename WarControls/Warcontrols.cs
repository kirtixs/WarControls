using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Xml;


using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

using System.Threading;

namespace PRoConEvents {

	public class WarControls : PRoConPluginAPI, IPRoConPluginInterface {

		//devsettings
		private bool isDebug = true;

		//time to react for the other team after the first ready
		private int readyCountdown = 30;

		//time until server will restart
		private int startCountdown = 10;

        //all users
		private string commandPrefix = "@";
		private string readyCommand = "ready";
        private string stopCommand = "stop";

		//internal vars
		private int team1ReadyTime = 0;
		private int team2ReadyTime = 0;

        private string language = "en";

        private bool doubleRestart = true;

		//@todo: read up on multidimensional lists... 
		private List<string> team1Players = new List<string>();
		private List<string> team2Players = new List<string>();

		public void init() {
		    this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
			this.Debug("WarControls enabled...");


        }

		public void destroy(){
			this.Debug("WarControls disabled...");
		}

		public override void OnGlobalChat(string playerName, string message) {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            string[] arguments = message.Split(' ');
			if(arguments[0].CompareTo(this.commandPrefix + this.readyCommand) == 0 && this.getNumberOfArguments(message) == 1){

                if(this.getTeamByPlayer(playerName) == 0) {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "Procon has no playerlist yet. Please try again in a few seconds.", "all");
                } else {
                    this.setReady(playerName);
                }
            } else if (arguments[0].CompareTo(this.commandPrefix + this.stopCommand) == 0 && this.getNumberOfArguments(message) == 1){
                this.stopMatch(playerName);
            }
		}

        public void stopMatch(string playerName) {
            this.ExecuteCommand("procon.protected.send", "admin.say", string.Format(this.getLocalizedString("TeamNotReady"), this.getTeamName(this.getTeamByPlayer(playerName))), "all");
        }

		public void setReady(string playerName){
			int teamID = this.getTeamByPlayer(playerName);
			int otherTeamID = 0;

			if(teamID == 1){
				otherTeamID = 2;
			} else {
				otherTeamID = 1;
			}

			int readyTime = this.getReadyTime(teamID);
			int otherReadyTime = this.getReadyTime(otherTeamID);
            int newReadyTime = this.getUnixTimeStamp();

            if(readyTime == 0 && otherReadyTime == 0) {
                //everything 0, just set readyTime
                this.setReadyTime(teamID);
                this.ExecuteCommand("procon.protected.send", "admin.say",string.Format(this.getLocalizedString("TeamReady"), playerName, this.getTeamName(teamID)) , "all");
            } else if(otherReadyTime > 0 && this.getUnixTimeStamp() - otherReadyTime <= this.startCountdown) {
                //other team is already ready and team is ready within time
                this.setReadyTime(teamID);
                this.ExecuteCommand("procon.protected.send", "admin.say",string.Format(this.getLocalizedString("TeamReady"), playerName, this.getTeamName(teamID)) , "all");
                Thread thread = new Thread(new ThreadStart(startMatch));
                thread.Start();

            } else if(otherReadyTime > 0 && this.getUnixTimeStamp() - otherReadyTime > this.startCountdown) {
                //team wasnt ready soon enough
                this.ExecuteCommand("procon.protected.send", "admin.say", string.Format(this.getLocalizedString("NotReadySoonEnough"), this.getTeamName(teamID)), "all");
                this.resetReadyTime();

            } else if(readyTime > 0 && this.getUnixTimeStamp() - readyTime <= this.startCountdown) {
                //team is already ready
                this.ExecuteCommand("procon.protected.send", "admin.say", string.Format(this.getLocalizedString("AlreadyReady"), this.getTeamName(teamID)), "all");
            }  else if(readyTime > 0 && this.getUnixTimeStamp() - readyTime > this.startCountdown) {
                this.ExecuteCommand("procon.protected.send", "admin.say", string.Format(this.getLocalizedString("NotReadySoonEnough"), this.getTeamName(otherTeamID)), "all");
                this.resetReadyTime();
    
            }
		}

        public void resetReadyTime(){
            this.team1ReadyTime = 0;
            this.team2ReadyTime = 0;
        }

        public string getTeamName(int teamID){
            if (teamID == 1){
                return "U.S. Army";
            } 
            return "Russian Army";   
        }

		public void startMatch(){
            this.resetReadyTime();
			for(int i = 0; i < this.startCountdown; i++){
				this.ExecuteCommand("procon.protected.send", "admin.yell", string.Format(this.getLocalizedString("Live"), this.startCountdown - i), "1000", "all");
               
			}
            this.ExecuteCommand("procon.protected.send", "admin.yell", this.getLocalizedString("StartMatch"), "1000", "all");
            Thread sleep = new Thread(new ThreadStart(sleepTimer));
            sleep.Start();
		}


        public void sleepTimer() {
            Thread.Sleep(15000);
            this.restartLevel();
        }


		public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset) {

			List<string> tmpTeam1Players = new List<string>();
			List<string> tmpTeam2Players = new List<string>();

			foreach(CPlayerInfo player in players){
					if(player.TeamID == 1){
						tmpTeam1Players.Add(player.SoldierName);
					} else if(player.TeamID == 2){
						tmpTeam2Players.Add(player.SoldierName);
					}
			}
			this.team1Players = tmpTeam1Players;
			this.team2Players = tmpTeam2Players;
		}

		public int getTeamByPlayer(string playerName){
			foreach(string player in team1Players){
				if(player == playerName){
                    return 1;
				}
			}
			foreach(string player in team2Players){
				if(player == playerName){
                    return 2;
				}
			}
            return 0;
		}

		public int getReadyTime(int teamID){
			if(teamID == 1){
				return this.team1ReadyTime;
			} 
			return this.team2ReadyTime;
		}

		public int getOtherTeamID(int teamID){
			if(teamID == 1){
				return 2;
			} 
			return 1;
		}

		public void setReadyTime(int teamID){
			if(teamID == 1){
				this.team1ReadyTime = this.getUnixTimeStamp();
			} else {
				this.team2ReadyTime = this.getUnixTimeStamp();
			}
		}

		public int getUnixTimeStamp(){
			TimeSpan _TimeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
			return unchecked((int)_TimeSpan.TotalSeconds);
		}


		public int getNumberOfArguments(string message){
			return message.Split(' ').Length;
		}

		public void Debug(string message){
			if(this.isDebug){
				this.LogWrite(message);
			}
		}

		public void LogWrite(String msg){
			this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
		}

        public string GetPluginName() {
            return "WarControls";
        }

        public string GetPluginVersion() {
            return "0.4b";
        }

        public string GetPluginAuthor() {
            return "redshark1802";
        }

        public string GetPluginWebsite() {
            return "github.com/redshark1802";
        }

        public string GetPluginDescription() {
            return @"<h2>WarControls</h2>";
		}

		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
			this.RegisterEvents(this.GetType().Name, "OnGlobalChat", "OnListPlayers", "OnLoadingLevel", "OnLevelLoaded"); 
		}

        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal){
            if(this.doubleRestart) {
                Thread.Sleep(10000);
                this.restartLevel();
                this.doubleRestart = false;
            } else {
                Thread.Sleep(10000);
                this.ExecuteCommand("procon.protected.send", "admin.say", this.getLocalizedString("MatchLive"), "all");
                this.doubleRestart = true;
            }
        }

        public void restartLevel() {
            this.ExecuteCommand("procon.protected.send", "mapList.restartRound");
        }

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

		public void OnPluginEnable() {
			this.init();
		}

		public void OnPluginDisable() {
			this.destroy();
		}

		public List<CPluginVariable> GetDisplayPluginVariables() {
			List<CPluginVariable> varDisplayList = new List<CPluginVariable>();

            varDisplayList.Add(new CPluginVariable("Timelimit for both teams to be ready", typeof(int), this.startCountdown));
            varDisplayList.Add(new CPluginVariable("Language", "enum.BasicEnumExampleCustomList("+string.Join("|", this.getLanguages())+")", this.language));
			
            return varDisplayList;
		}

		public List<CPluginVariable> GetPluginVariables() {
            return GetDisplayPluginVariables();
		}

		public void SetPluginVariable(string strVariable, string strValue) {

            if (strVariable == "Timelimit for both teams to be ready") {
                this.startCountdown = Convert.ToInt32(strValue);
            } else if(strVariable == "Language") {
                this.language = strValue;
            }
		}

        public string[] getLanguages() {

            string[] files = Directory.GetFiles(this.getLanguagePath(), "*.xml");

            string[] languages = new string[files.Length];

            int pos = 0;
            foreach(string file in files) {
                string lang = file.Replace(this.getLanguagePath(), "").Replace(".xml", "");
                languages[pos] = lang;
                pos++;
            }

            return languages;
        }

        public string getLocalizedString(string stringIdentifier) {

            string localizedString;
            localizedString = this.getXmlValue(stringIdentifier, this.language);

            if(localizedString == "" && this.language != "en") {
                localizedString = this.getXmlValue(stringIdentifier, "en");
            }
            return localizedString;
        }


        public string getXmlValue(string stringIdentifier, string language) {
            string localizedString = "";
            try {
                XmlDocument doc = new XmlDocument();
                doc.Load(this.getLanguagePath() + language + ".xml");

                XmlNodeList xnList = doc.SelectNodes("/LocalizedStrings");

                foreach(XmlNode xn in xnList) {
                    localizedString = xn[stringIdentifier].InnerText;
                }

                return localizedString;
            } catch(XmlException ex) {
                this.Debug(String.Format("XmlException: {0}", ex.Message));
            }
            return localizedString;
        }


        public string getLanguagePath() {

            return @".\Plugins\BF3\WarControls\lang\";
        }
	}
}