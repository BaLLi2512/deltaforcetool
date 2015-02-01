using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;


using Styx.Common.Helpers;
using Styx.CommonBot.Inventory;
using Styx.Helpers;

using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using Styx.Common;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx;

using Styx.CommonBot;
using Styx.CommonBot.Profiles;
/***************************************************************
TODO

-PetsAlive EnemyPetsAlive



***************************************************************/


using Styx.WoWInternals.World;
using Styx.WoWInternals.Misc;


using Bots.BGBuddy.Helpers;

using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using System.Drawing;




namespace PokehBuddy
{
    partial class configForm
    {
        int initdone = 0;
        private void BlacklistSave()
        {
            listBox3.Sorted = true;
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\blacklist.txt";

            StreamWriter Write;
            try
            {
                Write = new StreamWriter(filename);
                for (int I = 0; I < listBox3.Items.Count; I++)
                {
                    Write.WriteLine(Convert.ToString(listBox3.Items[I]));
                }
                Write.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
            PokehBuddy.BlacklistLoad();

        }
        private void MacroLoad()
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\logicmacro.txt";

            quickLogicListBox.Items.Clear();

            try
            {
                StreamReader Read = new StreamReader(Convert.ToString(filename));
                while (Read.Peek() >= 0)
                {
                    string pline = Read.ReadLine();
                    if (pline != null)
                    {
                        quickLogicListBox.Items.Add(pline);

                    }
                }
                Read.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }




        }

        private void MacroSave()
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\logicmacro.txt";

            quickLogicListBox.Sorted = true;


            StreamWriter Write;
            try
            {
                Write = new StreamWriter(filename);
                for (int I = 0; I < quickLogicListBox.Items.Count; I++)
                {
                    Write.WriteLine(Convert.ToString(quickLogicListBox.Items[I]));
                }
                Write.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }




        }
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void BlacklistLoad()
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\blacklist.txt";

            listBox3.Items.Clear();

            try
            {
                StreamReader Read = new StreamReader(Convert.ToString(filename));
                while (Read.Peek() >= 0)
                {
                    string pline = Read.ReadLine();
                    if (pline != null)
                    {
                        listBox3.Items.Add(pline.ToLower());

                    }
                }
                Read.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
            PokehBuddy.BlacklistLoad();



        }
        private void WhitelistSave()
        {
            listBox4.Sorted = true;
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\whitelist.txt";

            StreamWriter Write;
            try
            {
                Write = new StreamWriter(filename);
                for (int I = 0; I < listBox4.Items.Count; I++)
                {
                    Write.WriteLine(Convert.ToString(listBox4.Items[I]));
                }
                Write.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
            PokehBuddy.WhitelistLoad();

        }
        private void WhitelistLoad()
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\whitelist.txt";

            listBox4.Items.Clear();

            try
            {
                StreamReader Read = new StreamReader(Convert.ToString(filename));
                while (Read.Peek() >= 0)
                {
                    string pline = Read.ReadLine();
                    if (pline != null)
                    {
                        listBox4.Items.Add(pline.ToLower());

                    }
                }
                Read.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
            PokehBuddy.WhitelistLoad();


        }

        public static void Log(string s)
        {
            Logging.Write(s);
        }

        private void configForm_Load(object sender, EventArgs e)
        {
            LoadDefaults();
            BlacklistLoad();
            WhitelistLoad();
            MacroLoad();
            textBox3.Text = PokehBuddy.MySettings.HPFormula;
            textBox4.Text = PokehBuddy.MySettings.AdFormula;
            textBox5.Text = PokehBuddy.MySettings.DisFormula;
            textBox6.Text = PokehBuddy.MySettings.LevelFormula;

            useBlacklistCheckbox.Checked = PokehBuddy.MySettings.UseBlackList;
            useWhitelistCheckbox.Checked = PokehBuddy.MySettings.UseWhiteList;
            isPrimaryBotCheckBox.Checked = PokehBuddy.MySettings.isPrimaryType;


            forfeitIfUninterstingCheckBox.Checked = PokehBuddy.MySettings.ForfeitIfNotInteresting;

            useBandagesComboBox.SelectedIndex = PokehBuddy.MySettings.UseBandagesToHeal;
            usePetHealComboBox.SelectedIndex = PokehBuddy.MySettings.UseHealSkill;

            trackBar1.Value = PokehBuddy.MySettings.HPFactor;
            trackBar2.Value = PokehBuddy.MySettings.LevelFactor;
            trackBar3.Value = PokehBuddy.MySettings.AdFactor;
            trackBar4.Value = PokehBuddy.MySettings.DisFactor;
            distanceBar.Value = PokehBuddy.MySettings.Distance;

            checkBox1.Checked = PokehBuddy.MySettings.DoPVP;
            lowerLevelComboBox.SelectedIndex = PokehBuddy.MySettings.BelowLevel;
            higherLevelComboBox.SelectedIndex = PokehBuddy.MySettings.AboveLevel;
            pvpLowLevelComboBox.SelectedIndex = PokehBuddy.MySettings.PVPMinTime;

            doNotEngageLessThanComboBox.SelectedIndex = PokehBuddy.MySettings.MinPetsAlive - 1;

            pvpHighLevelComboBox.Items.Clear();
            int dummy = 0;
            for (int i = 1; i < 60; i++)
            {
                dummy = PokehBuddy.MySettings.PVPMinTime + 1;
                pvpHighLevelComboBox.Items.Add(dummy + i);
            }


            pvpHighLevelComboBox.SelectedIndex = PokehBuddy.MySettings.PVPMaxTime;


            catchWhenComboBox.SelectedIndex = PokehBuddy.MySettings.GetRarity - 1;
            initdone = 1;
            //Logging.Write("LAlalala");

            /* TheDungeonComboBox.SelectedIndex = PokehBuddy.MySettings.TheDungeon;
             HeartstoneOutSetting.Text = PokehBuddy.MySettings.HeartstoneAfter.ToString();
             WalkoutTimeSetting.Text = PokehBuddy.MySettings.WalkOutAfter.ToString();
             MailEveryResetCheck.Checked = PokehBuddy.MySettings.MailEveryReset;
             HordeCheck.Checked = PokehBuddy.MySettings.AmHorde;
             IBSupport.Checked = PokehBuddy.MySettings.IBSupport;*/
        }

        private void MailEveryResetCheck_CheckedChanged(object sender, EventArgs e)
        {
            //   PokehBuddy.MySettings.MailEveryReset = MailEveryResetCheck.Checked;
        }
        private void HordeCheck_CheckedChanged(object sender, EventArgs e)
        {
            //   PokehBuddy.MySettings.AmHorde = HordeCheck.Checked;
        }
        private void IBSupport_CheckedChanged(object sender, EventArgs e)
        {
            //   PokehBuddy.MySettings.IBSupport = IBSupport.Checked;
        }



        private void WalkoutTimeSetting_Leave(object sender, EventArgs e)
        {
            //  WalkoutTimeSetting.Text = PokehBuddy.MySettings.WalkOutAfter.ToString();
        }

        private void WalkoutTimeSetting_TextChanged(object sender, EventArgs e)
        {
            //   int n;
            //   int.TryParse(WalkoutTimeSetting.Text, out n);
            //   if (n < 1)
            //       n = 1;
            //   else if (n > 50)
            //       n = 50;
            //   PokehBuddy.MySettings.WalkOutAfter = n;
        }

        private void HeartstoneOutSetting_TextChanged(object sender, EventArgs e)
        {
            //   int n;
            //   int.TryParse(HeartstoneOutSetting.Text, out n);
            //  if (n < 20)
            //       n = 20;
            //   else if (n > 100)
            //      n = 100;
            //    PokehBuddy.MySettings.HeartstoneAfter = n;
        }

        private void HeartstoneOutSetting_Leave(object sender, EventArgs e)
        {
            //  HeartstoneOutSetting.Text = PokehBuddy.MySettings.HeartstoneAfter.ToString();
        }

        private void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            BlacklistSave();
            WhitelistSave();
            PokehBuddy.MySettings.HPFormula = textBox3.Text;
            PokehBuddy.MySettings.AdFormula = textBox4.Text;
            PokehBuddy.MySettings.DisFormula = textBox5.Text;
            PokehBuddy.MySettings.LevelFormula = textBox6.Text;

            PokehBuddy.MySettings.Save();
        }

        private void textBox3_TextChanged(object sender, System.EventArgs e)
        {

        }

        private void checkBox7_CheckedChanged(object sender, System.EventArgs e)
        {
            //textBox3.Enabled = checkBox7.Checked;
            //textBox4.Enabled = checkBox7.Checked;
            //textBox5.Enabled = checkBox7.Checked;
            //textBox6.Enabled = checkBox7.Checked;

        }

        private void checkBox9_CheckedChanged(object sender, System.EventArgs e)
        {
            petLogicOverwriteExistingCheckBox.Enabled = petLogicSaveSimilarSpeciesCheckBox.Checked;
        }

        private void button12_Click_1(object sender, System.EventArgs e)
        {
            /*//comboBox11.Text="SELECT ACTION";
            string macrostring = "Health(THISPET),ISLESSTHAN,NUMBER,0,25";
            string[] macrologic = macrostring.Split(',');

            comboBox2.Text = macrologic[0];
            comboBox3.Text = macrologic[1];
            comboBox4.Text = macrologic[2];
            textBox101.Text = macrologic[3];
            textBox102.Text = macrologic[4];

            button3.Enabled = true;
            button4.Enabled = true;*/
            quickLogicBox.Visible = !quickLogicBox.Visible;


        }

        private void button13_Click_1(object sender, System.EventArgs e)
        {
            quickLogicBox.Visible = false;
            groupBox4.Visible = false;
            listBox2.Visible = false;

        }

        private void button14_Click(object sender, System.EventArgs e)
        {
            if (comboBox15.SelectedIndex == -1) comboBox15.SelectedIndex = 0;
            if (comboBox16.SelectedIndex == -1) comboBox16.SelectedIndex = 0;
            loadpreviewimages();


            updatenumbers();







        }
        private int CalcLua(string s)
        {
            //if ("+s+") then return true end return false
            List<string> cnt = Lua.GetReturnValues("return (" + s + ")");

            //if (cnt[1] != "0") return true;
            //return false;
            return int.Parse(cnt[0]);
        }

        

        private void updatenumbers()
        {
            PokehBuddy.myPets.updateMyPets();
            var ce = new CalcEngine.CalcEngine();
            string s = "1 + 1 * 3";
            var x = ce.Parse(s);

            var value = x.Evaluate();
            int advantage = 0;
            int disadvantage = 0;
            var total = 0;


            //pet 1

            total = 0;
            disadvantage = 0;
            advantage = 0;

            s = textBox3.Text;
            s = s.Replace("petHP", PokehBuddy.myPets[0].Health.ToString()).Replace("HPFactor", trackBar1.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = int.Parse(value.ToString());
            label50.Text = value.ToString();
            Logging.Write("Lua test : " + CalcLua(s));


            int mypet = PokehBuddy.GetTypeByID(PokehBuddy.myPets[0].PetID.ToString());
            if (mypet == PokehBuddy.DumbChoiceTakeMoreDMG(comboBox16.SelectedIndex + 1)) disadvantage = -2;
            if (mypet == PokehBuddy.DumbChoiceDealLessDMG(comboBox16.SelectedIndex + 1)) disadvantage = disadvantage - 1;//rating -1;
            if (mypet == PokehBuddy.SmartChoiceTakeLessDMG(comboBox16.SelectedIndex + 1)) advantage = 1;
            if (mypet == PokehBuddy.SmartChoiceDealMoreDMG(comboBox16.SelectedIndex + 1)) advantage = advantage + 2;
            s = textBox4.Text;   //advantage * 50 * AdFactor
            s = s.Replace("advantage", advantage.ToString()).Replace("AdFactor", trackBar3.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label49.Text = value.ToString();

            s = textBox5.Text;   //advantage * 50 * AdFactor
            s = s.Replace("disadvantage", disadvantage.ToString()).Replace("DisFactor", trackBar4.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label48.Text = value.ToString();



            s = textBox6.Text;   //advantage * 50 * AdFactor
            s = s.Replace("petLevel", PokehBuddy.myPets[0].Level.ToString()).Replace("enemylevel", (comboBox15.SelectedIndex + 1).ToString()).Replace("LevelFactor", trackBar2.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label47.Text = value.ToString();

            label46.Text = total.ToString();

            //pet 2

            total = 0;
            disadvantage = 0;
            advantage = 0;

            s = textBox3.Text;
            s = s.Replace("petHP", PokehBuddy.myPets[1].Health.ToString()).Replace("HPFactor", trackBar1.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = int.Parse(value.ToString());
            label56.Text = value.ToString();


            mypet = PokehBuddy.GetTypeByID(PokehBuddy.myPets[1].PetID.ToString());
            if (mypet == PokehBuddy.DumbChoiceTakeMoreDMG(comboBox16.SelectedIndex + 1)) disadvantage = -2;
            if (mypet == PokehBuddy.DumbChoiceDealLessDMG(comboBox16.SelectedIndex + 1)) disadvantage = disadvantage - 1;//rating -1;
            if (mypet == PokehBuddy.SmartChoiceTakeLessDMG(comboBox16.SelectedIndex + 1)) advantage = 1;
            if (mypet == PokehBuddy.SmartChoiceDealMoreDMG(comboBox16.SelectedIndex + 1)) advantage = advantage + 2;
            s = textBox4.Text;   //advantage * 50 * AdFactor
            s = s.Replace("advantage", advantage.ToString()).Replace("AdFactor", trackBar3.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label55.Text = value.ToString();

            s = textBox5.Text;   //advantage * 50 * AdFactor
            s = s.Replace("disadvantage", disadvantage.ToString()).Replace("DisFactor", trackBar4.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label54.Text = value.ToString();



            s = textBox6.Text;   //advantage * 50 * AdFactor
            s = s.Replace("petLevel", PokehBuddy.myPets[1].Level.ToString()).Replace("enemylevel", (comboBox15.SelectedIndex + 1).ToString()).Replace("LevelFactor", trackBar2.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label53.Text = value.ToString();

            label52.Text = total.ToString();

            //pet 3

            total = 0;
            disadvantage = 0;
            advantage = 0;

            s = textBox3.Text;
            s = s.Replace("petHP", PokehBuddy.myPets[2].Health.ToString()).Replace("HPFactor", trackBar1.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = int.Parse(value.ToString());
            label62.Text = value.ToString();


            mypet = PokehBuddy.GetTypeByID(PokehBuddy.myPets[2].PetID.ToString());
            if (mypet == PokehBuddy.DumbChoiceTakeMoreDMG(comboBox16.SelectedIndex + 1)) disadvantage = -2;
            if (mypet == PokehBuddy.DumbChoiceDealLessDMG(comboBox16.SelectedIndex + 1)) disadvantage = disadvantage - 1;//rating -1;
            if (mypet == PokehBuddy.SmartChoiceTakeLessDMG(comboBox16.SelectedIndex + 1)) advantage = 1;
            if (mypet == PokehBuddy.SmartChoiceDealMoreDMG(comboBox16.SelectedIndex + 1)) advantage = advantage + 2;
            s = textBox4.Text;   //advantage * 50 * AdFactor
            s = s.Replace("advantage", advantage.ToString()).Replace("AdFactor", trackBar3.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label60.Text = value.ToString();

            s = textBox5.Text;   //advantage * 50 * AdFactor
            s = s.Replace("disadvantage", disadvantage.ToString()).Replace("DisFactor", trackBar4.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label59.Text = value.ToString();



            s = textBox6.Text;   //advantage * 50 * AdFactor
            s = s.Replace("petLevel", PokehBuddy.myPets[2].Level.ToString()).Replace("enemylevel", (comboBox15.SelectedIndex + 1).ToString()).Replace("LevelFactor", trackBar2.Value.ToString());
            x = ce.Parse(s);
            value = x.Evaluate();
            total = total + int.Parse(value.ToString());
            label58.Text = value.ToString();

            label57.Text = total.ToString();


        }
        private void loadpreviewimages()
        {

            Logging.Write("loading image :" + ImgUrl(1));
            pictureBox6.ImageLocation = ImgUrl(1);

            Logging.Write("loading image :" + ImgUrl(2));
            pictureBox7.ImageLocation = ImgUrl(2);

            Logging.Write("loading image :" + ImgUrl(3));
            pictureBox8.ImageLocation = ImgUrl(3);


        }
        private string ImgUrl(int slotnum)
        {

            string theicon = PokehBuddy.SlotIcon(slotnum);
            string baseurl = "http://wow.zamimg.com/images/wow/icons/large/";
            string replace1 = @"INTERFACE\ICONS\";

            string image = theicon.Replace(replace1, "").Replace(".BLP", "").ToLower();
            image = baseurl + image + ".jpg";
            return image;
        }

        private void trackBar2_Scroll(object sender, System.EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, System.EventArgs e)
        {

        }

        private void button18_Click(object sender, System.EventArgs e)
        {
            listBox3.Items.Add(textBox7.Text);

        }

        private void button20_Click(object sender, System.EventArgs e)
        {
            listBox4.Items.Add(textBox7.Text);
        }

        private void button17_Click(object sender, System.EventArgs e)
        {
            if (listBox3.SelectedIndex > -1)
            {
                listBox3.Items.RemoveAt(listBox3.SelectedIndex);
            }
        }

        private void button19_Click(object sender, System.EventArgs e)
        {
            if (listBox4.SelectedIndex > -1)
            {
                listBox4.Items.RemoveAt(listBox4.SelectedIndex);
            }
        }

        private void button26_Click(object sender, System.EventArgs e)
        {
            LoadDefaults();

        }
        private void LoadDefaults()
        {
            listBox6.Items.Clear();
            PokehBuddy.LoadDefaultLogic("Default Logic");
            string dumdum = PokehBuddy.DefaultLogicz.Logic;
            string[] PetLogics = dumdum.Split('@');
            foreach (string alogic in PetLogics)
            {
                listBox6.Items.Add(alogic);
            }

        }


        private void button23_Click(object sender, System.EventArgs e)
        {
            if (listBox6.SelectedIndex > -1)
            {
                listBox6.Items.RemoveAt(listBox6.SelectedIndex);
            }
        }

        private void button25_Click(object sender, System.EventArgs e)
        {
            SaveDefault();
        }
        private void SaveDefault()
        {
            int i = 0;
            string dummy = "";
            foreach (object item in listBox6.Items)
            {
                dummy = dummy + item.ToString();
                if (i < listBox6.Items.Count - 1) dummy = dummy + "@";
                i++;
            }
            PokehBuddy.LoadDefaultLogic("Default Logic");
            PokehBuddy.DefaultLogicz.Logic = dummy;
            PokehBuddy.DefaultLogicz.Save();
        }

        private void button21_Click(object sender, System.EventArgs e)
        {
            MoveItemPetRotate(-1);
        }

        private void button22_Click(object sender, System.EventArgs e)
        {
            MoveItemPetRotate(1);
        }

        private void comboBox14_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            PokehBuddy.MySettings.MinPetsAlive = doNotEngageLessThanComboBox.SelectedIndex + 1;

        }

        private void comboBox12_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            PokehBuddy.MySettings.UseBandagesToHeal = useBandagesComboBox.SelectedIndex;
        }

        private void comboBox10_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            PokehBuddy.MySettings.UseHealSkill = usePetHealComboBox.SelectedIndex;
        }

        private void textBox7_Enter(object sender, System.EventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, System.EventArgs e)
        {

        }

        private void button28_Click(object sender, System.EventArgs e)
        {
            if (petLogicListBox.SelectedIndex > -1)
            {

                listBox6.Items.Add(petLogicListBox.SelectedItem.ToString());
                petLogicListBox.Items.Remove(petLogicListBox.SelectedItem);
            }
        }

        private void label17_Click(object sender, System.EventArgs e)
        {

        }

        private void checkBox5_CheckedChanged(object sender, System.EventArgs e)
        {
            PokehBuddy.MySettings.UseBlackList = useBlacklistCheckbox.Checked;

        }

        private void checkBox6_CheckedChanged(object sender, System.EventArgs e)
        {
            PokehBuddy.MySettings.UseWhiteList = useWhitelistCheckbox.Checked;
        }

        private void button15_Click(object sender, System.EventArgs e)
        {
            if (comboBox13.Text == "default leveling")
            {


                textBox3.Text = "petHP * HPFactor";
                textBox4.Text = "advantage * 50 * AdFactor";
                textBox5.Text = "disadvantage * 50 * DisFactor";
                textBox6.Text = "(petLevel - enemylevel) * 4 * LevelFactor";

                trackBar1.Value = 8;
                trackBar2.Value = -50;
                trackBar3.Value = 4;
                trackBar4.Value = 4;








            }
        }

        private void trackBar1_Scroll(object sender, System.EventArgs e)
        {

        }

        private void trackBar3_Scroll(object sender, System.EventArgs e)
        {

        }

        private void trackBar4_Scroll(object sender, System.EventArgs e)
        {

        }

        //private void pictureBox1_DoubleClick(object sender, System.EventArgs e)
        //{
            //button12.Enabled = true;
        //}

        private void button30_Click(object sender, System.EventArgs e)
        {
            //comboBox11.Text="SELECT ACTION";
            string macrostring = "Health(THISPET),ISLESSTHAN,NUMBER,0,25";
            //string[] macrologic = macrostring.Split(',');

            macrostring = logicWhenComboBox.Text + "," +
            logicCompareComboBox.Text + "," +
            logicValueComboBox.Text + "," +
            logicWhenTextBox.Text + "," +
            logicValueTextBox.Text;
            quickLogicListBox.Items.Add(macrostring);
            MacroSave();

        }
        private void SetBnB()
        {
            logicWhenComboBox.Enabled = false;
            logicCompareComboBox.Enabled = false;
            logicValueComboBox.Enabled = false;
            logicAddButton.Enabled = false;
            logicAndButton.Enabled = false;
            logicFinishButton.Enabled = false;

            if (logicActionComboBox.SelectedIndex > -1)
            {
                logicWhenComboBox.Enabled = true;
            }
            if (logicWhenComboBox.Text != "")
            {
                logicCompareComboBox.Enabled = true;
                logicValueComboBox.Enabled = true;
            }
            if (logicValueComboBox.Text != "")
            {
                logicAndButton.Enabled = true;
                if (logicActionComboBox.SelectedIndex > -1 && logicWhenComboBox.Text != "" && logicCompareComboBox.Text != "" && logicValueComboBox.Text != "") logicFinishButton.Enabled = true;

            }
            if (logicFinishTextBox.Text != "") logicAddButton.Enabled = true;

        }

        private void listBox7_SelectedIndexChanged(object sender, System.EventArgs e)
        {

        }

        private void listBox7_DoubleClick(object sender, System.EventArgs e)
        {
            if (quickLogicListBox.SelectedIndex > -1)
            {

                string macrostring = quickLogicListBox.SelectedItem.ToString();
                string[] macrologic = macrostring.Split(',');

                logicWhenComboBox.Text = macrologic[0];
                logicCompareComboBox.Text = macrologic[1];
                logicValueComboBox.Text = macrologic[2];
                logicWhenTextBox.Text = macrologic[3];
                logicValueTextBox.Text = macrologic[4];

                logicAndButton.Enabled = true;
                logicFinishButton.Enabled = true;
                SetBnB();
            }
        }

        //private void pictureBox1_Click(object sender, System.EventArgs e)
        //{

        //}

        private void button24_Click(object sender, System.EventArgs e)
        {
            if (listBox6.SelectedIndex == -1) return;
            string value = listBox6.SelectedItem.ToString();
            if (InputBox("Edit Logic", "Logic:", ref value) == DialogResult.OK)
            {
                int index = listBox6.SelectedIndex;
                listBox6.Items.RemoveAt(index);
                listBox6.Items.Insert(index, value);

            }
        }

        private void button27_Click(object sender, System.EventArgs e)
        {
            if (petLogicListBox.SelectedIndex == -1) return;
            string value = petLogicListBox.SelectedItem.ToString();
            if (InputBox("Edit Logic", "Logic:", ref value) == DialogResult.OK)
            {
                int index = petLogicListBox.SelectedIndex;
                petLogicListBox.Items.RemoveAt(index);
                petLogicListBox.Items.Insert(index, value);

            }
        }

        private void button29_Click(object sender, System.EventArgs e)
        {
            quickLogicListBox.Items.RemoveAt(quickLogicListBox.SelectedIndex);
            MacroSave();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            PokehBuddy.MySettings.isPrimaryType = isPrimaryBotCheckBox.Checked;
        }
    }

}

namespace PokehBuddy
{
    public partial class configForm : Form
    {
        string[] options = { "COOLDOWN(SKILL(1))","EQUALS", "true,false",
	"COOLDOWN(SKILL(2))","EQUALS", "true,false", 
	"COOLDOWN(SKILL(3))","EQUALS", "true,false", 
	"Health(THISPET)","EQUALS,ISLESSTHAN,ISGREATERTHAN", "NUMBER,Health(ENEMYPET)", 
	"Health(ENEMYPET)","EQUALS,ISLESSTHAN,ISGREATERTHAN", "NUMBER,Health(THISPET)", 
	"MyPetLevel","EQUALS,ISLESSTHAN,ISGREATERTHAN", "NUMBER,EnemyPetLevel,EnemyPetLevel + NUMBER", 
	"EnemyPetLevel","EQUALS,ISLESSTHAN,ISGREATERTHAN", "NUMBER,MyPetLevel,MyPetLevel + NUMBER", 
	"ENEMYTYPE","EQUALS,ISNOT", "HUMANOID,DRAGONKIN,FLYING,UNDEAD,CRITTER,MAGIC,ELEMENTAL,BEAST,AQUATIC,MECHANICAL", 
	"HASBUFF(X)","EQUALS", "true,false", 
	"HASENEMYBUFF(X)","EQUALS","true,false",  
	"WEATHERBUFF(X)","EQUALS", "true,false", 
	"HASTEAMBUFF(X)","EQUALS", "true,false", 
	"ENEMYTEAMBUFF(X)","EQUALS","true,false", 
	"MYPETSPEED","EQUALS,ISLESSTHAN,ISGREATERTHAN","NUMBER,ENEMYSPEED", 
	"ENEMYSPEED","EQUALS,ISLESSTHAN,ISGREATERTHAN","NUMBER,MYPETSPEED",
"MyPetsAlive","EQUALS,ISLESSTHAN,ISGREATERTHAN","NUMBER,EnemyPetsAlive",
"EnemyPetsAlive","EQUALS,ISLESSTHAN,ISGREATERTHAN","NUMBER,MyPetsAlive"};


        public configForm()
        {
            InitializeComponent();
            //listBox2.Visible = false;
            //this.pictureBox1.Image = new Bitmap(Application.StartupPath + "\\Bots\\PokehBuddy\\Images\\pb.jpg");
            this.pictureBox9.Image = new Bitmap(Application.StartupPath + "\\Bots\\PokehBuddy\\Images\\Broom.png");
            //this.pictureBox9.Image = new Bitmap(Application.StartupPath + "\\Bots\\PokehBuddy\\Images\\info.png");
        }

        private void TheDungeonComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //      PokehBuddy.MySettings.TheDungeon = TheDungeonComboBox.SelectedIndex;
            //Log("Dungeon " + TheDungeonComboBox.SelectedIndex);



        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            PokehBuddy.MySettings.GetRarity = catchWhenComboBox.SelectedIndex + 1;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            label1.Text = trackBar1.Value.ToString();
            PokehBuddy.MySettings.HPFactor = trackBar1.Value;



        }
        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            label2.Text = trackBar2.Value.ToString();
            PokehBuddy.MySettings.LevelFactor = trackBar2.Value;



        }
        private void trackBar3_ValueChanged(object sender, EventArgs e)
        {

            label3.Text = trackBar3.Value.ToString();
            PokehBuddy.MySettings.AdFactor = trackBar3.Value;



        }
        private void trackBar4_ValueChanged(object sender, EventArgs e)
        {
            label4.Text = trackBar4.Value.ToString();
            PokehBuddy.MySettings.DisFactor = trackBar4.Value;



        }
        private void trackBar5_ValueChanged(object sender, EventArgs e)
        {
            label10.Text = distanceBar.Value.ToString();
            PokehBuddy.MySettings.Distance = distanceBar.Value;



        }
        private void ListBuffs_Clicked(object sender, EventArgs e)
        {
            Lua.DoString("for j=1,C_PetBattles.GetNumAuras(1,C_PetBattles.GetActivePet(1)) do  local buffid = C_PetBattles.GetAuraInfo(1,1,j)  print (buffid) end");


        }


        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (logicActionComboBox.SelectedIndex == -1)
            {
                logicAndButton.Enabled = false;
                logicFinishButton.Enabled = false;
                logicWhenComboBox.Enabled = false;
                logicCompareComboBox.Enabled = false;
                logicValueComboBox.Enabled = false;
                logicWhenComboBox.Items.Clear();
                logicCompareComboBox.Items.Clear();
                logicValueComboBox.Items.Clear();
            }
            if (logicActionComboBox.SelectedIndex > -1)
            {
                logicWhenComboBox.Enabled = true;
                if (logicCompareComboBox.Text == "") logicCompareComboBox.Enabled = false;
                if (logicValueComboBox.Text == "") logicValueComboBox.Enabled = false;

                if (logicWhenComboBox.Text == "") logicWhenComboBox.Items.Clear();

                for (int i = 0; i < options.Count(); i++)
                {
                    logicWhenComboBox.Items.Add(options[i]);
                    i++;
                    i++;
                }
            }
            SetBnB();



        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            logicCompareComboBox.Enabled = true;
            logicCompareComboBox.Text = "";
            logicCompareComboBox.Items.Clear();

            logicValueComboBox.Enabled = true;
            logicValueComboBox.Text = "";
            logicValueComboBox.Items.Clear();


            string dumdumdum = logicWhenComboBox.Text;
            //Logging.Write("im here!!!"+dumdumdum);
            for (int i = 0; i < options.Count(); i++)
            {
                if (dumdumdum.Contains(options[i]))
                {

                    string[] equalizers = options[i + 1].Split(',');

                    foreach (string equalizer in equalizers)
                    {
                        logicCompareComboBox.Items.Add(equalizer);
                    }
                    string[] targetz = options[i + 2].Split(',');

                    foreach (string targ in targetz)
                    {
                        logicValueComboBox.Items.Add(targ);
                    }

                }
                i++;
                i++;
            }






        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (logicValueComboBox.Text.Contains("NUMBER"))
            {
                if (logicActionComboBox.Text != "" && logicWhenComboBox.Text != "" && logicCompareComboBox.Text != "" && logicValueComboBox.Text != "")
                {
                    logicAndButton.Enabled = true;
                    logicFinishButton.Enabled = true;
                }


            }




            if (logicValueComboBox.Text.Contains("true"))
            {

                if (logicActionComboBox.Text != "" && logicWhenComboBox.Text != "" && logicCompareComboBox.Text != "" && logicValueComboBox.Text != "")
                {
                    logicAndButton.Enabled = true;
                    logicFinishButton.Enabled = true;
                }


            }
            if (logicValueComboBox.Text.Contains("false"))
            {


                if (logicActionComboBox.Text != "" && logicWhenComboBox.Text != "" && logicCompareComboBox.Text != "" && logicValueComboBox.Text != "")
                {
                    logicAndButton.Enabled = true;
                    logicFinishButton.Enabled = true;
                }


            }

            if (logicActionComboBox.SelectedIndex > -1)
            {
                if (logicActionComboBox.Text != "" && logicWhenComboBox.Text != "" && logicCompareComboBox.Text != "" && logicValueComboBox.Text != "")
                {
                    logicAndButton.Enabled = true;
                    logicFinishButton.Enabled = true;
                }

            }




        }
        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //AND
            string dummy = logicWhenComboBox.Text.Replace("(X)", "(" + logicWhenTextBox.Text + ")") + " " + logicCompareComboBox.Text + " " + logicValueComboBox.Text.Replace("NUMBER", "" + logicValueTextBox.Text + "") + " $ ";
            logicAndTextBox.Text = logicAndTextBox.Text + dummy;
            //comboBox11.Enabled = false;
            logicWhenComboBox.SelectedIndex = -1;
            logicCompareComboBox.SelectedIndex = -1;
            logicValueComboBox.SelectedIndex = -1;
            logicAndButton.Enabled = false;
            logicFinishButton.Enabled = false;
            logicAddButton.Enabled = false;

            logicFinishButton.PerformClick();

        }

        private void button4_Click(object sender, EventArgs e)
        {
            //FINISH
            string dummy = logicActionComboBox.Text + " " + logicAndTextBox.Text + logicWhenComboBox.Text.Replace("(X)", "(" + logicWhenTextBox.Text + ")") + " " + logicCompareComboBox.Text + " " + logicValueComboBox.Text.Replace("NUMBER", "" + logicValueTextBox.Text + "");
            logicFinishTextBox.Text = dummy;
            logicAndTextBox.Text = "";
            logicWhenComboBox.SelectedIndex = -1;
            logicCompareComboBox.SelectedIndex = -1;
            logicValueComboBox.SelectedIndex = -1;
            logicActionComboBox.SelectedIndex = -1;
            logicWhenComboBox.SelectedIndex = -1;
            logicCompareComboBox.SelectedIndex = -1;
            logicValueComboBox.SelectedIndex = -1;
            logicAddButton.Enabled = true;
            logicAddButton.PerformClick();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            petLogicListBox.Items.Add(logicFinishTextBox.Text);
            logicFinishTextBox.Text = "";
            logicAddButton.Enabled = false;

        }

        private void button9_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            //Lua.DoString("for j=1,C_PetBattles.GetNumAuras(1,C_PetBattles.GetActivePet(1)) do  local buffid = C_PetBattles.GetAuraInfo(1,C_PetBattles.GetActivePet(1),j)  print (buffid) end");
            List<string> cnt2 = Lua.GetReturnValues("local dummy = '' for j=1,C_PetBattles.GetNumAuras(1,C_PetBattles.GetActivePet(1)) do  local buffid = C_PetBattles.GetAuraInfo(1,C_PetBattles.GetActivePet(1),j)  dummy = tostring(dummy) .. tostring(buffid) .. ',' end return dummy");
            string[] buffz = cnt2[0].Split(',');

            foreach (string buff in buffz)
            {
                listBox2.Items.Add(buff);
            }
            if (listBox2.Items.Count > 0) listBox2.Visible = true;


        }
        private void button99_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            //Lua.DoString("for i=1, C_PetBattles.GetNumAuras(0,0) do local auraID = C_PetBattles.GetAuraInfo(LE_BATTLE_PET_WEATHER, PET_BATTLE_PAD_INDEX, i) print(auraID) end");
            List<string> cnt2 = Lua.GetReturnValues("local dummy = '' for j=1,C_PetBattles.GetNumAuras(LE_BATTLE_PET_WEATHER,PET_BATTLE_PAD_INDEX) do  local buffid = C_PetBattles.GetAuraInfo(LE_BATTLE_PET_WEATHER,PET_BATTLE_PAD_INDEX,j)  dummy = tostring(dummy) .. tostring(buffid) .. ',' end return dummy");
            string[] buffz = cnt2[0].Split(',');
            listBox2.Items.Clear();
            foreach (string buff in buffz)
            {
                listBox2.Items.Add(buff);
            }
            if (listBox2.Items.Count > 0) listBox2.Visible = true;
        }

        private void button919_Click(object sender, EventArgs e)
        {//               for i=1, C_PetBattles.GetNumAuras(1,0) do local auraID = C_PetBattles.GetAuraInfo(1, PET_BATTLE_PAD_INDEX, i) print(auraID) end
            listBox2.Items.Clear();
            //Lua.DoString("for i=1, C_PetBattles.GetNumAuras(1,0) do local auraID = C_PetBattles.GetAuraInfo(1, PET_BATTLE_PAD_INDEX, i) print(auraID) end");
            List<string> cnt2 = Lua.GetReturnValues("local dummy = '' for j=1,C_PetBattles.GetNumAuras(1,0) do  local buffid = C_PetBattles.GetAuraInfo(1,PET_BATTLE_PAD_INDEX,j)  dummy = tostring(dummy) .. tostring(buffid) .. ',' end return dummy");
            string[] buffz = cnt2[0].Split(',');
            listBox2.Items.Clear();
            foreach (string buff in buffz)
            {
                listBox2.Items.Add(buff);
            }
            if (listBox2.Items.Count > 0) listBox2.Visible = true;


        }
        private void button929_Click(object sender, EventArgs e)
        {
            //get whole pet list
            /*		for (int intI = 1; intI < 597; intI++) {
                    List<string> cntlist = Lua.GetReturnValues("local stor = '' local petID, speciesID, _, _, _, _, _, name, icon  = C_PetJournal.GetPetInfoByIndex("+ intI +", false); stor = stor .. '*'..speciesID .. '*,*' .. name ..'*,*' .. icon .. '*,'  return stor");

                    Logging.Write(cntlist[0]);
                    }*/

            //


            listBox2.Items.Clear();
            //Lua.DoString("for i=1, C_PetBattles.GetNumAuras(2,0) do local auraID = C_PetBattles.GetAuraInfo(2, PET_BATTLE_PAD_INDEX, i) print(auraID) end");
            List<string> cnt2 = Lua.GetReturnValues("local dummy = '' for j=1,C_PetBattles.GetNumAuras(2,0) do  local buffid = C_PetBattles.GetAuraInfo(2,PET_BATTLE_PAD_INDEX,j)  dummy = tostring(dummy) .. tostring(buffid) .. ',' end return dummy");
            string[] buffz = cnt2[0].Split(',');
            listBox2.Items.Clear();
            foreach (string buff in buffz)
            {
                listBox2.Items.Add(buff);
            }
            if (listBox2.Items.Count > 0) listBox2.Visible = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            //tabControl1.Visible=false;

            //Lua.DoString("for j=1,C_PetBattles.GetNumAuras(2,C_PetBattles.GetActivePet(2)) do  local buffid = C_PetBattles.GetAuraInfo(2,C_PetBattles.GetActivePet(2),j)  print (buffid) end");
            List<string> cnt2 = Lua.GetReturnValues("local dummy = '' for j=1,C_PetBattles.GetNumAuras(2,C_PetBattles.GetActivePet(2)) do  local buffid = C_PetBattles.GetAuraInfo(2,C_PetBattles.GetActivePet(2),j)  dummy = tostring(dummy) .. tostring(buffid) .. ',' end return dummy");
            string[] buffz = cnt2[0].Split(',');
            listBox2.Items.Clear();
            foreach (string buff in buffz)
            {
                listBox2.Items.Add(buff);
            }
            if (listBox2.Items.Count > 0) listBox2.Visible = true;
        }


        public void MoveItemPetRotate(int direction)
        {
            // Checking selected item
            if (petOrderListBox.SelectedItem == null || petOrderListBox.SelectedIndex < 0)
                return; // No selected item - nothing to do

            // Calculate new index using move direction
            int newIndex = petOrderListBox.SelectedIndex + direction;

            // Checking bounds of the range
            if (newIndex < 0 || newIndex >= petOrderListBox.Items.Count)
                return; // Index out of range - nothing to do

            object selected = petOrderListBox.SelectedItem;

            // Removing removable element
            petOrderListBox.Items.Remove(selected);
            // Insert it in new position
            petOrderListBox.Items.Insert(newIndex, selected);
            // Restore selection
            petOrderListBox.SetSelected(newIndex, true);
        }
        public void MoveItem(int direction)
        {
            // Checking selected item
            if (petLogicListBox.SelectedItem == null || petLogicListBox.SelectedIndex < 0)
                return; // No selected item - nothing to do

            // Calculate new index using move direction
            int newIndex = petLogicListBox.SelectedIndex + direction;

            // Checking bounds of the range
            if (newIndex < 0 || newIndex >= petLogicListBox.Items.Count)
                return; // Index out of range - nothing to do

            object selected = petLogicListBox.SelectedItem;

            // Removing removable element
            petLogicListBox.Items.Remove(selected);
            // Insert it in new position
            petLogicListBox.Items.Insert(newIndex, selected);
            // Restore selection
            petLogicListBox.SetSelected(newIndex, true);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + petIDNumberLabel.Text + ".xml";
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            petLogicListBox.Items.Clear();
            PokehBuddy.LoadPetSettings(petIDNumberLabel.Text, label22.Text);

            //string dumdum = "";
            string dumdum = PokehBuddy.PetSettings.Logic;
            string[] PetLogics = dumdum.Split('@');
            foreach (string alogic in PetLogics)
            {
                petLogicListBox.Items.Add(alogic);
            }


        }
        private void button5_Click(object sender, EventArgs e)
        {

            MoveItem(-1);

        }

        private void button6_Click(object sender, EventArgs e)
        {

            MoveItem(1);

        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (petLogicListBox.SelectedIndex > -1) petLogicListBox.Items.Remove(petLogicListBox.SelectedItem);

        }
        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            logicWhenTextBox.Text = listBox2.Text;
            listBox2.Visible = false;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (refreshPetComboBox.SelectedIndex > -1)
            {
                petLogicListBox.Items.Clear();
                PokehBuddy.myPets.updateMyPets();
                PokehBuddy.LoadPetSettings(PokehBuddy.myPets[refreshPetComboBox.SelectedIndex].PetID.ToString(), PokehBuddy.myPets[refreshPetComboBox.SelectedIndex].SpeciesID.ToString());
                label22.Text = PokehBuddy.myPets[refreshPetComboBox.SelectedIndex].SpeciesID.ToString();
                petIDNumberLabel.Text = PokehBuddy.myPets[refreshPetComboBox.SelectedIndex].PetID.ToString();
                //string dumdum = "";

                string dumdum = PokehBuddy.PetSettings.Logic;
                string[] PetLogics = dumdum.Split('@');
                foreach (string alogic in PetLogics)
                {
                    petLogicListBox.Items.Add(alogic);
                }

                string theicon = PokehBuddy.SlotIcon(refreshPetComboBox.SelectedIndex + 1);
                string baseurl = "http://wow.zamimg.com/images/wow/icons/large/";
                string replace1 = @"INTERFACE\ICONS\";

                string image = theicon.Replace(replace1, "").Replace(".BLP", "").ToLower();
                image = baseurl + image + ".jpg";
                Logging.Write("loading image :" + image);
                petPictureBox.ImageLocation = image;
                /*//5.1//
                label20.Text=PokehBuddy.GetNameByID(label71.Text);
                List<string> cnt3 = Lua.GetReturnValues("local speciesID, customName, _, _, _,_, name, icon  = C_PetJournal.GetPetInfoByPetID(string.format('%X',"+label71.Text+")) if customName==nil then return 'No custom name' end	return customName");
                label22.Text=cnt3[0];
                //List<string> cnt4 = Lua.GetReturnValues("local speciesID, customName, _, _, _,_, name, icon, petType  = C_PetJournal.GetPetInfoByPetID(string.format('%X',"+label71.Text+")) return petType");
                //label22.Text=cnt4[0];
                5.1*/

            }
        }
        private string GetPetImage(int slot)
        {
            List<string> cnt2 = Lua.GetReturnValues("local petid = C_PetJournal.GetPetLoadOutInfo(" + slot + ") local speciesID, customName, _, _, _,_, name, icon  = C_PetJournal.GetPetInfoByPetID(petid) return icon");
            string baseurl = "http://wow.zamimg.com/images/wow/icons/large/";
            string replace1 = @"INTERFACE\ICONS\";

            string image = cnt2[0].Replace(replace1, "").Replace(".BLP", "").ToLower();
            image = baseurl + image + ".jpg";
            return (image);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (petLogicSaveCheckBox.Checked)
            {
                SavePetSpecific();
            }


            if (petLogicSaveSimilarSpeciesCheckBox.Checked)
            {
                SaveSpecies(); //Save Species & Similar
            }
            if (petLogicSaveSimilarSpeciesCheckBox.Checked && petLogicOverwriteExistingCheckBox.Checked)
            {
                SaveNOverwrite(); //Save Species & Similar & Overwrite
            }



        }
        private void SavePetSpecific()
        {
            if (petIDNumberLabel.Text != "")
            {
                string dummy = "";
                int i = 0;
                foreach (object item in petLogicListBox.Items)
                {
                    dummy = dummy + item.ToString();
                    if (i < petLogicListBox.Items.Count - 1) dummy = dummy + "@";
                    i++;
                }
                //local petID, ability1ID, ability2ID, ability3ID, locked = C_PetJournal.GetPetLoadOutInfo(i) return ability1ID, ability2ID, ability3ID



                PokehBuddy.LoadPetSettings(petIDNumberLabel.Text, label22.Text);
                List<string> cnt1 = Lua.GetReturnValues("for i=1,3 do local petID, ability1ID, ability2ID, ability3ID, locked = C_PetJournal.GetPetLoadOutInfo(i) if (tonumber(petID,16)==" + petIDNumberLabel.Text + ") then return ability1ID, ability2ID, ability3ID end end return 0,0,0");
                string spelllayout = "ASSIGNABILITY1(" + cnt1[0] + ")@" +
                                     "ASSIGNABILITY2(" + cnt1[1] + ")@" +
                                     "ASSIGNABILITY3(" + cnt1[2] + ")";
                PokehBuddy.PetSettings.SpellLayout = spelllayout;
                PokehBuddy.PetSettings.Logic = dummy;
                PokehBuddy.PetSettings.Save();

            }
        }

        private void SaveSpecies()
        {
            if (petIDNumberLabel.Text != "")
            {
                string dummy = "";

                int i = 0;
                foreach (object item in petLogicListBox.Items)
                {
                    dummy = dummy + item.ToString();
                    if (i < petLogicListBox.Items.Count - 1) dummy = dummy + "@";
                    i++;
                }
                PokehBuddy.LoadPetSettingsBN(label22.Text);

                List<string> cntskillz = Lua.GetReturnValues("for i=1,3 do local petID, ability1ID, ability2ID, ability3ID, locked = C_PetJournal.GetPetLoadOutInfo(i) if (tonumber(petID,16)==" + petIDNumberLabel.Text + ") then return ability1ID, ability2ID, ability3ID end end return 0,0,0");
                string spelllayout = "ASSIGNABILITY1(" + cntskillz[0] + ")@" +
                                     "ASSIGNABILITY2(" + cntskillz[1] + ")@" +
                                     "ASSIGNABILITY3(" + cntskillz[2] + ")";
                PokehBuddy.PetSettings.SpellLayout = spelllayout;



                PokehBuddy.PetSettings.Logic = dummy;
                PokehBuddy.PetSettings.Save();







                Lua.DoString("C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_FAVORITES, false) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_COLLECTED, true) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_NOT_COLLECTED, true) ");
                Lua.DoString("C_PetJournal.ClearSearchFilter() C_PetJournal.AddAllPetSourcesFilter() C_PetJournal.AddAllPetTypesFilter() ");
                ////Lua.DoString("C_PetJournal.SetSearchFilter('" + petname + "')");
                List<string> cnt1 = Lua.GetReturnValues("local teller=0 local retdata={} retdata[0]='nothing' retdata[1]='nothing'  local dummy1 = '" + PokehBuddy.GetSpeciesByName(label22.Text) + "' local numpets = C_PetJournal.GetNumPets(false) local skillist = C_PetJournal.GetPetAbilityList(dummy1); for j = 1, numpets do  local _, dummy2 = C_PetJournal.GetPetInfoByIndex(j,false); local skillist2 = C_PetJournal.GetPetAbilityList(dummy2); if skillist[1] == skillist2[1] and skillist[2] == skillist2[2] and skillist[3] == skillist2[3] then  local _,_,_,_,_,_,_,ass = C_PetJournal.GetPetInfoByIndex(j,false) teller=teller+1 retdata[teller]=ass end end return teller");
                int getal = 0;
                try
                {
                    getal = Convert.ToInt32(cnt1[0]);
                }
                catch (Exception exc)
                {
                    Log(exc.Message.ToString());
                }
                for (int intI = 1; intI < getal; intI++)
                {
                    List<string> cnt = Lua.GetReturnValues("local teller=0 local retdata={} retdata[0]='nothing' retdata[1]='nothing'  local dummy1 = '" + PokehBuddy.GetSpeciesByName(label22.Text) + "' local numpets = C_PetJournal.GetNumPets(false) local skillist = C_PetJournal.GetPetAbilityList(dummy1); for j = 1, numpets do  local _, dummy2 = C_PetJournal.GetPetInfoByIndex(j,false); local skillist2 = C_PetJournal.GetPetAbilityList(dummy2); if skillist[1] == skillist2[1] and skillist[2] == skillist2[2] and skillist[3] == skillist2[3] then  local _,speciesID,_,_,_,_,_,ass = C_PetJournal.GetPetInfoByIndex(j,false) teller=teller+1 retdata[teller]=speciesID end end return (retdata[" + intI + "])");
                    cnt[0] = PokehBuddy.GetNameBySpeciesID(cnt[0]);
                    string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + cnt[0] + ".xml";
                    if (!File.Exists(filename))
                    {

                        string filename2 = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + label22.Text + ".xml";
                        if (File.Exists(filename2))
                        {
                            File.Copy(filename2, filename);
                        }
                    }



                    Logging.Write(cnt[0]);
                }




                // Log(cnt[0]);
                //if (cnt[0]=="1") dummy=true;






            }
        }
        private void SaveNOverwrite()
        {
            if (petIDNumberLabel.Text != "")
            {
                string dummy = "";

                int i = 0;
                foreach (object item in petLogicListBox.Items)
                {
                    dummy = dummy + item.ToString();
                    if (i < petLogicListBox.Items.Count - 1) dummy = dummy + "@";
                    i++;
                }

                PokehBuddy.LoadPetSettingsBN(label22.Text);

                List<string> cntskillz = Lua.GetReturnValues("for i=1,3 do local petID, ability1ID, ability2ID, ability3ID, locked = C_PetJournal.GetPetLoadOutInfo(i) if (tonumber(petID,16)==" + petIDNumberLabel.Text + ") then return ability1ID, ability2ID, ability3ID end end return 0,0,0");
                string spelllayout = "ASSIGNABILITY1(" + cntskillz[0] + ")@" +
                                     "ASSIGNABILITY2(" + cntskillz[1] + ")@" +
                                     "ASSIGNABILITY3(" + cntskillz[2] + ")";
                PokehBuddy.PetSettings.SpellLayout = spelllayout;



                PokehBuddy.PetSettings.Logic = dummy;
                PokehBuddy.PetSettings.Save();







                Lua.DoString("C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_FAVORITES, false) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_COLLECTED, true) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_NOT_COLLECTED, true) ");
                Lua.DoString("C_PetJournal.ClearSearchFilter() C_PetJournal.AddAllPetSourcesFilter() C_PetJournal.AddAllPetTypesFilter() ");
                ////Lua.DoString("C_PetJournal.SetSearchFilter('" + petname + "')");
                List<string> cnt1 = Lua.GetReturnValues("local teller=0 local retdata={} retdata[0]='nothing' retdata[1]='nothing'  local dummy1 = C_PetJournal.GetPetInfoByPetID(string.format('%X'," + petIDNumberLabel.Text + ")); local numpets = C_PetJournal.GetNumPets(false) local skillist = C_PetJournal.GetPetAbilityList(dummy1); for j = 1, numpets do  local _, dummy2 = C_PetJournal.GetPetInfoByIndex(j,false); local skillist2 = C_PetJournal.GetPetAbilityList(dummy2); if skillist[1] == skillist2[1] and skillist[2] == skillist2[2] and skillist[3] == skillist2[3] then  local _,_,_,_,_,_,_,ass = C_PetJournal.GetPetInfoByIndex(j,false) teller=teller+1 retdata[teller]=ass end end return teller");
                int getal = 0;
                try
                {
                    getal = Convert.ToInt32(cnt1[0]);
                }
                catch (Exception exc)
                {
                    Log(exc.Message.ToString());
                }
                Lua.DoString("C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_FAVORITES, false) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_COLLECTED, true) C_PetJournal.SetFlagFilter(LE_PET_JOURNAL_FLAG_NOT_COLLECTED, true) ");
                Lua.DoString("C_PetJournal.ClearSearchFilter() C_PetJournal.AddAllPetSourcesFilter() C_PetJournal.AddAllPetTypesFilter() ");
                for (int intI = 1; intI < getal; intI++)
                {
                    List<string> cnt = Lua.GetReturnValues("local teller=0 local retdata={} retdata[0]='nothing' retdata[1]='nothing'  local dummy1 = '" + PokehBuddy.GetSpeciesByName(label22.Text) + "' local numpets = C_PetJournal.GetNumPets(false) local skillist = C_PetJournal.GetPetAbilityList(dummy1); for j = 1, numpets do  local _, dummy2 = C_PetJournal.GetPetInfoByIndex(j,false); local skillist2 = C_PetJournal.GetPetAbilityList(dummy2); if skillist[1] == skillist2[1] and skillist[2] == skillist2[2] and skillist[3] == skillist2[3] then  local _,speciesID,_,_,_,_,_,ass = C_PetJournal.GetPetInfoByIndex(j,false) teller=teller+1 retdata[teller]=speciesID  end end return (retdata[" + intI + "])");
                    //Logging.Write(cnt[0]);
                    cnt[0] = PokehBuddy.GetNameBySpeciesID(cnt[0]);
                    string filename = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + cnt[0] + ".xml";
                    string filename2 = Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\" + label22.Text + ".xml";
                    //Logging.Write("File 1 : "+filename+ " File 2 :"+filename2);
                    if (File.Exists(filename) && filename != filename2) File.Delete(filename);

                    //string filename2=Application.StartupPath + "\\Bots\\PokehBuddy\\PetSettings\\"+PokehBuddy.GetNameByID(label71.Text)+".xml";
                    if (File.Exists(filename2) && filename != filename2)
                    {
                        File.Copy(filename2, filename);
                    }




                    Logging.Write(cnt[0]);
                }




                // Log(cnt[0]);
                //if (cnt[0]=="1") dummy=true;






            }

        }


        private void button44_Click(object sender, EventArgs e)
        {


        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            PokehBuddy.MySettings.ForfeitIfNotInteresting = forfeitIfUninterstingCheckBox.Checked;

        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            PokehBuddy.MySettings.BelowLevel = lowerLevelComboBox.SelectedIndex;


            /*            comboBox6.Items.Clear();
                        int dummy=0;
                        for (int i = 1; i < 60; i++)
                        {
                            dummy=comboBox5.SelectedIndex+1;
                            comboBox6.Items.Add(dummy + i);
                        }*/
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            PokehBuddy.MySettings.AboveLevel = higherLevelComboBox.SelectedIndex;
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            PokehBuddy.MySettings.PVPMinTime = pvpLowLevelComboBox.SelectedIndex;
            if (initdone == 1)
            {
                pvpHighLevelComboBox.Items.Clear();
                int dummy = 0;
                for (int i = 1; i < 60; i++)
                {
                    dummy = pvpLowLevelComboBox.SelectedIndex + 1;
                    pvpHighLevelComboBox.Items.Add(dummy + i);
                }
                pvpHighLevelComboBox.SelectedIndex = 0;
            }

        }
        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initdone == 1) PokehBuddy.MySettings.PVPMaxTime = pvpHighLevelComboBox.SelectedIndex;
            if (initdone == 0) pvpHighLevelComboBox.SelectedIndex = PokehBuddy.MySettings.PVPMaxTime;
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            PokehBuddy.MySettings.DoPVP = checkBox1.Checked;
        }
        private void button13_Click(object sender, EventArgs e)
        {
            listBox2.Visible = false;
            groupBox4.Visible = false;

        }

        private void button12_Click(object sender, EventArgs e)
        {
            /*5.1
            //PokehBuddy pok = new PokehBuddy();
            int i=0;
               for (i=1;i<4;i++){
                List<string> cnt2 = Lua.GetReturnValues("local dummy1 = C_PetJournal.GetPetInfoByPetID(string.format('%X',"+label71.Text+")); local skillist = C_PetJournal.GetPetAbilityList(dummy1) name, icon = C_PetJournal.GetPetAbilityInfo(skillist["+i+"]); return icon");
                string baseurl="http://wow.zamimg.com/images/wow/icons/large/";
                string replace1=@"INTERFACE\ICONS\";
			
                string image = cnt2[0].Replace(replace1,"").Replace(".BLP","").ToLower();
                image=baseurl+image+".jpg";
                if (i==1) pictureBox3.ImageLocation=image;
                if (i==2) pictureBox4.ImageLocation=image;
                if (i==3) pictureBox5.ImageLocation=image;
                //Logging.Write("loading image" + image);
                List<string> cnt3 = Lua.GetReturnValues("local dummy1 = C_PetJournal.GetPetInfoByPetID(string.format('%X',"+label71.Text+")); local skillist = C_PetJournal.GetPetAbilityList(dummy1) name, icon = C_PetJournal.GetPetAbilityInfo(skillist["+i+"]); return name");
                if (i==1) label23.Text=cnt3[0];
                if (i==2) label28.Text=cnt3[0];
                if (i==3) label32.Text=cnt3[0];
			
            }
            label24.Visible=false;
            label25.Visible=false;
            label26.Visible=false;
            label27.Visible=false;
            label30.Visible=false;
            label29.Visible=false;
		
			
			
                //List<string> cnt3 = Lua.GetReturnValues("local speciesID, customName, _, _, _,_, name, icon  = C_PetJournal.GetPetInfoByPetID("+label71.Text+") if customName==nil then return 'No custom name' end	return customName");
                //label22.Text=cnt3[0];
                groupBox4.Visible=true;
            5.1*/
        }


        private void button45_Click(object sender, EventArgs e)
        {






        }

    }









}




