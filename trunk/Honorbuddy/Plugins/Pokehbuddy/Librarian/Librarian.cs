using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SQLite;
using Styx.WoWInternals;
using Styx.Common;

namespace Pokehbuddyplug.Librarian
{
    public partial class Librarian : Form
    {
        public Librarian()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            doNonQuery("UPDATE SkillLibrary SET Pets = '' WHERE 1");
            
            try
            {
                List<string> cnt = Lua.GetReturnValues("totalpets,_=C_PetJournal.GetNumPets() return totalpets");
                StringBuilder sb = new StringBuilder();

                for (int i = 1; i <= Int32.Parse(cnt[0]); i++)
                {
                    Logging.Write("Importing pet skills from index "+i);
                    List<string> cnt2 = Lua.GetReturnValues("_,species,_,_,_,_,_,name,url=C_PetJournal.GetPetInfoByIndex(" + i.ToString() + ") return species");
                    List<string> skillids = Lua.GetReturnValues("idTable, levelTable = C_PetJournal.GetPetAbilityList(" + cnt2[0] + ") if (idTable[1] ~= null) then return idTable[1],idTable[2],idTable[3],idTable[4],idTable[5],idTable[6] else return 0,0,0,0,0,0 end;");
                    if (skillids[0] != "0")
                    {
                        foreach (string s in skillids)
                        {
                            InsertSkill(s, cnt2[0]);
                        }
                    }


                }
            }
            catch (Exception ee)
            {
                Logging.Write("" + ee.ToString());

            }




        }

        private void InsertSkill(string SkillID, string PetID)
        {
            if (!CheckForDouble(SkillID))
            {
                string q = "INSERT INTO SkillLibrary(SkillID, Pets, Logic) VALUES('" + SkillID + "',' ',' ')";
                doNonQuery(q);

            }

            string qq = "SELECT Pets FROM SkillLibrary WHERE SkillID = '" + SkillID + "'";

            string cs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";
            string cel1 = "";

            using (SQLiteConnection con = new SQLiteConnection(cs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(qq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            cel1 = rdr.GetString(0);
                        }
                    }
                }

                con.Close();
            }
            string tussenstring = "";
            if (cel1 != " " && cel1 != "")
            {
                
                tussenstring = ",";
            }
            if (cel1 == " ") cel1 = "";
            string final = cel1;
            if (!cel1.Contains(PetID)) final = cel1 + tussenstring + PetID;
            string qqq = @"UPDATE SkillLibrary 
            SET Pets = '" + final + @"' 
            
            Where SkillID = '" + SkillID + "'";
            doNonQuery(qqq);

            qqq = @"UPDATE SkillLibrary SET Prio = 0 WHERE Prio IS NULL";
            doNonQuery(qqq);
            qqq = @"UPDATE SkillLibrary SET Cooldown = 0 WHERE Cooldown IS NULL";
            doNonQuery(qqq);


        }



        private void doNonQuery(string qq)
        {

            string cs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";
            

                using (SQLiteConnection con = new SQLiteConnection(cs))
                {
                    con.Open();

                    using (SQLiteCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = qq;
                        cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }

            }



        private bool CheckForDouble(string spellid)
        {


            string qq = "SELECT * FROM SkillLibrary WHERE SkillID = '" + spellid + "'";

            string cs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";

            using (SQLiteConnection con = new SQLiteConnection(cs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(qq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            return true;
                        }
                    }
                }

                con.Close();
            }
           
            return false;

        }
        private string PetSpellName(string SpellID)
        {
            string luastring = "_, name, icon,_,_,_,_,_ = C_PetBattles.GetAbilityInfoByID(" + SpellID + ") return name"; //,icon)
            List<string> cnt = Lua.GetReturnValues(luastring);
            try
            {
                return cnt[0];
            }
            catch (Exception exc) { }


            return "error";
        }
        public string SearchLib(string SkillID)
        {

            string aqq = "SELECT Logic FROM SkillLibrary WHERE SkillID = '" + SkillID + "'";

            string acs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";

            using (SQLiteConnection con = new SQLiteConnection(acs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(aqq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            return rdr.GetString(0).TrimStart().TrimEnd();

                        }
                    }
                }

                con.Close();
            } 

            return "Error";
        }
        public int GetSpellPrio(string SkillID)
        {

            string aqq = "SELECT Prio FROM SkillLibrary WHERE SkillID = '" + SkillID + "'";

            string acs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";

            using (SQLiteConnection con = new SQLiteConnection(acs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(aqq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            return rdr.GetInt32(0);

                        }
                    }
                }

                con.Close();
            }

            return 0;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            if (comboBox2.Text=="") comboBox2.Text="ORDER BY SkillID";
            string qq = "SELECT SkillID FROM SkillLibrary WHERE 1 "+comboBox2.Text+";";

            string cs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";

            using (SQLiteConnection con = new SQLiteConnection(cs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(qq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            listBox1.Items.Add(rdr.GetString(0));
                        }
                    }
                }

                con.Close();
            }
           

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            label2.Text = PetSpellName(listBox1.SelectedItem.ToString());
            string aqq = "SELECT Logic FROM SkillLibrary WHERE SkillID = '" + listBox1.SelectedItem.ToString() + "'";

            string acs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";
            
            using (SQLiteConnection con = new SQLiteConnection(acs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(aqq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            textBox1.Text = rdr.GetString(0);

                        }
                    }
                }

                con.Close();
            }
            aqq = "SELECT Cooldown FROM SkillLibrary WHERE SkillID = '" + listBox1.SelectedItem.ToString() + "'";

            acs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";

            using (SQLiteConnection con = new SQLiteConnection(acs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(aqq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            label5.Text = "Cooldown : " + rdr.GetInt32(0).ToString() + " turns"; ;

                        }
                    }
                }

                con.Close();
            }
            
            numericUpDown1.Value = GetSpellPrio(listBox1.SelectedItem.ToString());

   
            
            string qq = "SELECT Pets FROM SkillLibrary WHERE SkillID = '" + listBox1.SelectedItem.ToString() + "'";

            string cs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";
            string petlist = "";
            using (SQLiteConnection con = new SQLiteConnection(cs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(qq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            petlist = rdr.GetString(0);

                        }
                    }
                }

                con.Close();
            }
            string[] Petz = petlist.Split(',');
            foreach (string s in Petz)
            {
                listBox2.Items.Add(Pokehbuddy.GetNameBySpeciesID(s));
                
            }

        }
        public string GeneratePuppyLogic(string Skillset)
        {
            return "SWAPOUT HASBUFF(248) EQUALS false $ TURNNUMBER ISGREATERTHAN 1@CASTSPELL(1) COOLDOWN(SKILL(1)) EQUALS false";
        }
        public string GenerateLogic(string Skillset)
        {
            string[] skillz = Skillset.Split('@');
            string[] gotskills = { "", "", "" };

            foreach (string alogic in skillz)
            {
                if ((String.Compare(alogic.Substring(0, 15), "ASSIGNABILITY1(") == 0))
                {
                    int FirstChr = alogic.IndexOf("ASSIGNABILITY1(") + 15;
                    int SecondChr = alogic.IndexOf(")", FirstChr);
                    string strTemp = alogic.Substring(FirstChr, SecondChr - FirstChr);

                    gotskills[0] = strTemp;
                }
                if ((String.Compare(alogic.Substring(0, 15), "ASSIGNABILITY2(") == 0))
                {
                    int FirstChr = alogic.IndexOf("ASSIGNABILITY2(") + 15;
                    int SecondChr = alogic.IndexOf(")", FirstChr);
                    string strTemp = alogic.Substring(FirstChr, SecondChr - FirstChr);

                    gotskills[1] = strTemp;
                }
                if ((String.Compare(alogic.Substring(0, 15), "ASSIGNABILITY3(") == 0))
                {
                    int FirstChr = alogic.IndexOf("ASSIGNABILITY3(") + 15;
                    int SecondChr = alogic.IndexOf(")", FirstChr);
                    string strTemp = alogic.Substring(FirstChr, SecondChr - FirstChr);

                    gotskills[2] = strTemp;
                }
            }
            if (gotskills[0] == "0") return "SWAPOUT Health(THISPET) ISLESSTHAN 30@CASTSPELL(1) COOLDOWN(SKILL(1)) EQUALS false";
            int[] priolist = {0,0,0};
            priolist[0] = GetSpellPrio(gotskills[0]);
            priolist[1] = GetSpellPrio(gotskills[1]);
            priolist[2] = GetSpellPrio(gotskills[2]);

            
            string dummy = "SWAPOUT Health(THISPET) ISLESSTHAN 25";
            dummy = dummy + "@" + GenFromLib(gotskills, priolist).Replace(gotskills[2], "3").Replace(gotskills[1], "2").Replace(gotskills[0], "1");
            dummy = dummy + "@CASTSPELL(1) COOLDOWN(SKILL(1)) EQUALS false@CASTSPELL(2) COOLDOWN(SKILL(2)) EQUALS false@CASTSPELL(3) COOLDOWN(SKILL(3)) EQUALS false";
            return dummy.Replace("@ @", "@").Replace("@ @", "@").Replace("@@", "@").Replace("@@", "@");
            
        }
        private string GenFromLib(string[] skills, int[] prio)
        {
            string blabla = "";
            int i = 0;
            foreach (string s in skills)
            {
                i++;
                blabla = blabla + "SkillID = '" + s+"' ";
                if (i < 3)
                {
                    blabla = blabla + "OR ";
                }

            }
            string qq = "SELECT Logic, SkillID FROM SkillLibrary WHERE "+blabla+"ORDER BY Prio DESC";
            string cs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";
            string petlist = "";
            i = 0;
            using (SQLiteConnection con = new SQLiteConnection(cs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(qq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            i++;
                            string oldpetlist = petlist;
                            petlist = petlist + rdr.GetString(0).Replace("**sn**", rdr.GetString(1));
                            if (i < 3 && oldpetlist !=petlist) petlist = petlist + "@";

                        }
                    }
                }

                con.Close();
            }
            return petlist;




            return "error";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int debuffnum = int.Parse(listBox1.SelectedItem.ToString())-1;
            textBox1.Text = textBox1.Text.TrimStart().TrimEnd().Replace("CASTSPELL(1)", "CASTSPELL(**sn**)").Replace("CASTSPELL(2)", "CASTSPELL(**sn**)").Replace("CASTSPELL(3)", "CASTSPELL(**sn**)")
                .Replace("COOLDOWN(SKILL(1))", "COOLDOWN(SKILL(**sn**))")
                .Replace("COOLDOWN(SKILL(2))", "COOLDOWN(SKILL(**sn**))")
                .Replace("COOLDOWN(SKILL(3))", "COOLDOWN(SKILL(**sn**))")
                .Replace("debuffnum", debuffnum.ToString());
            
            //textBox1.Text = textBox1.Text.Replace("CASTSPELL(1)", "CASTSPELL(**sn**)").Replace("CASTSPELL(2)", "CASTSPELL(**sn**)").Replace("CASTSPELL(3)", "CASTSPELL(**sn**)");
                /*.Replace("MyPetLevel ISLESSTHAN 10", "MyPetLevel ISLESSTHAN **ln**")
                .Replace("MyPetLevel ISLESSTHAN 15", "MyPetLevel ISLESSTHAN **ln**")
                .Replace("MyPetLevel ISLESSTHAN 20", "MyPetLevel ISLESSTHAN **ln**")
                .Replace("MyPetLevel ISGREATERTHANEQUALS 10", "MyPetLevel ISGREATERTHANEQUALS **ln**")
                .Replace("MyPetLevel ISGREATERTHANEQUALS 15", "MyPetLevel ISGREATERTHANEQUALS **ln**")
                .Replace("MyPetLevel ISGREATERTHANEQUALS 20", "MyPetLevel ISGREATERTHANEQUALS **ln**");*/

            
            string qqq = @"UPDATE SkillLibrary 
            SET Logic = '" + textBox1.Text + @"', 
            Prio = " + numericUpDown1.Value.ToString() + @" 
            Where SkillID = '" + listBox1.SelectedItem + "'";
            doNonQuery(qqq);

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            numericUpDown1.Value = comboBox1.SelectedIndex * 100;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //id, name, icon, maxCooldown, unparsedDescription, numTurns, petType, noStrongWeakHints = C_PetBattles.GetAbilityInfoByID(abilityID)
            string qq = "SELECT SkillID FROM SkillLibrary WHERE 1";
            string cs = "URI=file:" + Application.StartupPath + "\\Plugins\\Pokehbuddy\\Librarian\\Library.db";
            List<string> todo = new List<string>();
            using (SQLiteConnection con = new SQLiteConnection(cs))
            {
                con.Open();



                using (SQLiteCommand cmd = new SQLiteCommand(qq, con))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string dummy=rdr.GetString(0);
                            string luastring = "id, name, icon, maxCooldown, unparsedDescription, numTurns, petType, noStrongWeakHints = C_PetBattles.GetAbilityInfoByID(" + dummy + ") return maxCooldown";
                            List<string> cnt = Lua.GetReturnValues(luastring);
                            string qqq = "UPDATE SkillLibrary SET Cooldown= '"+cnt[0]+"' WHERE SkillID='"+dummy+"';";
                            todo.Add(qqq);
                            
                             
                             
                            

                        }
                    }
                }

                con.Close();
            }
            foreach (string s in todo)
            {
                doNonQuery(s);
            }
        }

        private void Librarian_Load(object sender, EventArgs e)
        {

        }
        

    }
}
//FORFEIT MyPetLevel ISLESSTHANEQUALS 26
/*


"MyPetLevel ISLESSTHAN 10"
"MyPetLevel ISLESSTHAN 15"
"MyPetLevel ISLESSTHAN 20"

MyPetLevel ISGREATERTHANEQUALS 10
MyPetLevel ISGREATERTHANEQUALS 15
MyPetLevel ISGREATERTHANEQUALS 20

ISGREATERTHAN











*/