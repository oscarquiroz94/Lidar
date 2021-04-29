using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace ControlGimbalLIDAR
{
    public partial class PPAL : Form
    {
        ThreadStart childref;
        ThreadStart childref_2;
        Thread childThread;
        Thread TimerThread;

        SerialPort ArduinoPort = new SerialPort();

        public delegate void delegadoEscribir(String msg);
        private delegate void getComboItem();
        private delegate void showhidebotones();
        private delegate void enadisabotones(bool state, bool azimut);

        delegadoEscribir salidaConsola;
        delegadoEscribir salidaSTEP;
        delegadoEscribir salidaAnguloAzimut;
        delegadoEscribir salidaAnguloCenit;
        delegadoEscribir salidaAzimutRPM;
        delegadoEscribir salidaCenitRPM;
        delegadoEscribir salidaDelayStepAzimut;
        delegadoEscribir salidaDelayStepCenit;
        getComboItem itemcomboAzimut;
        getComboItem itemcomboCenit;
        showhidebotones hidebutons;
        showhidebotones showbutons;
        enadisabotones enadisaSET;
        enadisabotones enadisaStart;


        private bool buttonDown;
        double pasosPorRevolucionAzimut;
        double pasosPorRevolucionCenit;
        double ratioAzimut;
        double ratioCenit;
        double microStepAzimut;
        double microStepCenit;
        bool onConnected = false;
        bool enterKey = false;
        bool enableleft = false;
        bool enableright = false;
        bool enableup = false;
        bool enabledown = false;
        bool ceroAzimut = false;
        bool ceroCenit = false;
        bool lockMode = true;
  
        ulong tiempo = 0;
        ulong tiempoFinal = 0;
        ulong ciclosParos = 0;

        double anguloStepAzimut = 0.0;
        double anguloStepCenit = 0.0;
        string timesSelectedAzimut = "1";
        string timesSelectedCenit = "1";

        public PPAL()
        {
            childref = new ThreadStart(HiloConfigInicial);
            childThread = new Thread(childref);
            childThread.Start();

            salidaConsola = new delegadoEscribir(EscribirConsola);
            salidaSTEP = new delegadoEscribir(EscribirSTEP);
            salidaAnguloAzimut = new delegadoEscribir(EscribirAnguloAzimut);
            salidaAnguloCenit = new delegadoEscribir(EscribirAnguloCenit);
            salidaAzimutRPM = new delegadoEscribir(EscribirAzimutRPM);
            salidaCenitRPM = new delegadoEscribir(EscribirCenitRPM);
            salidaDelayStepAzimut = new delegadoEscribir(EscribirDelayStepAzimut);
            salidaDelayStepCenit = new delegadoEscribir(EscribirDelayStepCenit);
            itemcomboAzimut = new getComboItem(GetTimesAzi);
            itemcomboCenit = new getComboItem(GetTimesZen);
            showbutons = new showhidebotones(ShowALLbutonStep);
            hidebutons = new showhidebotones(HideALLbutonStep);
            enadisaSET = new enadisabotones(BotonSET);
            enadisaStart = new enadisabotones(BotonStart);

            ArduinoPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);


            InitializeComponent();

            listaTimesAzi.SelectedIndex = 0;
            listaTimesZen.SelectedIndex = 0;

            campoAnguloAzimut.SelectionAlignment = HorizontalAlignment.Center;
            campoAnguloCenit.SelectionAlignment = HorizontalAlignment.Center;
            campoAzimutRPM.SelectionAlignment = HorizontalAlignment.Center;
            campoCenitRPM.SelectionAlignment = HorizontalAlignment.Center;
            campoDelayOnStep_Azi.SelectionAlignment = HorizontalAlignment.Center;
            campoDelayOnStep_Zen.SelectionAlignment = HorizontalAlignment.Center;
            campoStepAngle_Azi.SelectionAlignment = HorizontalAlignment.Center;
            campoStepAngle_Zen.SelectionAlignment = HorizontalAlignment.Center;
            campoStep.SelectionAlignment = HorizontalAlignment.Center;

            botoncancelar.Visible = false;
            botonlock.Visible = false;
            botonSetAzimutCero.Visible = false;
            botonSetCenitCero.Visible = false;
            botonStartPoint.Visible = true; 

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                listaPuertos.Items.Add(port);
            }

            //Actualizar fromulario
            Application.DoEvents();

        }

        public void HiloTimer()
        {
            this.tiempo = 0;
            while (true)
            {
                Thread.Sleep(1000);
                if (this.enableleft)
                {
                    this.tiempo++;
                    this.campoDelayOnStep_Azi.Invoke(salidaDelayStepAzimut, new object[] { (this.tiempoFinal - this.tiempo).ToString() }); //escribir tiempo en campo
                    if (this.tiempoFinal == this.tiempo) //El hilo sigue vivo mientras los tiempos son diferentes, cuenta regresiva
                    {
                        this.ciclosParos++;
                        this.campoDelayOnStep_Azi.Invoke(salidaDelayStepAzimut, new object[] { this.tiempoFinal.ToString() }); //escribir tiempo en campo
                        this.listaTimesAzi.Invoke(itemcomboAzimut); //leer cantidad de ciclos de combobox
                        if (Convert.ToUInt64(timesSelectedAzimut) == this.ciclosParos)
                        {
                            this.Invoke(showbutons);
                            this.enableleft = false;
                        }
                        else
                        {
                            SendStepAngle_AzimutLeft();
                        }
                        break;
                    }
                    else if (this.tiempoFinal == 0) //Si no se asigno tiempo de espera el hilo muere
                    {
                        this.campoDelayOnStep_Azi.Invoke(salidaDelayStepAzimut, new object[] { "0" });
                        this.Invoke(showbutons);
                        this.enableleft = false;
                        break;
                    }
                }
                else if (this.enableright)
                {
                    this.tiempo++;
                    this.campoDelayOnStep_Azi.Invoke(salidaDelayStepAzimut, new object[] { (this.tiempoFinal - this.tiempo).ToString() }); //escribir tiempo en campo
                    if (this.tiempoFinal == this.tiempo) //El hilo sigue vivo mientras los tiempos son diferentes, cuenta regresiva
                    {
                        this.ciclosParos++;
                        this.campoDelayOnStep_Azi.Invoke(salidaDelayStepAzimut, new object[] { this.tiempoFinal.ToString() }); //escribir tiempo en campo
                        this.listaTimesAzi.Invoke(itemcomboAzimut);  //leer cantidad de ciclos de combobox
                        if (Convert.ToUInt64(timesSelectedAzimut) == this.ciclosParos)
                        {
                            this.Invoke(showbutons);
                            this.enableright = false;
                        }
                        else
                        {
                            SendStepAngle_AzimutRight();
                        }
                        break;
                    }
                    else if (this.tiempoFinal == 0) //Si no se asigno tiempo de espera el hilo muere
                    {
                        this.campoDelayOnStep_Azi.Invoke(salidaDelayStepAzimut, new object[] { "0"});
                        this.Invoke(showbutons);
                        this.enableright = false;
                        break;
                    }
                }
                else if (this.enableup)
                {
                    this.tiempo++;
                    this.campoDelayOnStep_Zen.Invoke(salidaDelayStepCenit, new object[] { (this.tiempoFinal - this.tiempo).ToString() }); //escribir tiempo en campo
                    if (this.tiempoFinal == this.tiempo) //El hilo sigue vivo mientras los tiempos son diferentes, cuenta regresiva
                    {
                        this.ciclosParos++;
                        this.campoDelayOnStep_Zen.Invoke(salidaDelayStepCenit, new object[] { this.tiempoFinal.ToString() }); //escribir tiempo en campo
                        this.listaTimesZen.Invoke(itemcomboCenit);  //leer cantidad de ciclos de combobox
                        if (Convert.ToUInt64(timesSelectedCenit) == this.ciclosParos)
                        {
                            this.Invoke(showbutons);
                            this.enableup = false;
                        }
                        else
                        {
                            SendStepAngle_CenitUp();
                        }
                        break;
                    }
                    else if (this.tiempoFinal == 0) //Si no se asigno tiempo de espera el hilo muere
                    {
                        this.campoDelayOnStep_Zen.Invoke(salidaDelayStepCenit, new object[] { "0" });
                        this.Invoke(showbutons);
                        this.enableup = false;
                        break;
                    }
                }
                else if (this.enabledown)
                {
                    this.tiempo++;
                    this.campoDelayOnStep_Zen.Invoke(salidaDelayStepCenit, new object[] { (this.tiempoFinal - this.tiempo).ToString() }); //escribir tiempo en campo
                    if (this.tiempoFinal == this.tiempo)  //El hilo sigue vivo mientras los tiempos son diferentes, cuenta regresiva
                    {
                        this.ciclosParos++;
                        this.campoDelayOnStep_Zen.Invoke(salidaDelayStepCenit, new object[] { this.tiempoFinal.ToString() }); //escribir tiempo en campo
                        this.listaTimesZen.Invoke(itemcomboCenit);  //leer cantidad de ciclos de combobox
                        if (Convert.ToUInt64(timesSelectedCenit) == this.ciclosParos)
                        {
                            this.Invoke(showbutons);
                            this.enabledown = false;
                        }
                        else
                        {
                            SendStepAngle_CenitDown();
                        }
                        break;
                    }
                    else if (this.tiempoFinal == 0) //Si no se asigno tiempo de espera el hilo muere
                    {
                        this.campoDelayOnStep_Zen.Invoke(salidaDelayStepCenit, new object[] { "0" });
                        this.Invoke(showbutons);
                        this.enabledown = false;
                        break;
                    }

                }
                else
                {
                    break;
                }
            }
        }

        public void HiloConfigInicial()
        {
            while (!onConnected) Thread.Sleep(1000);
            Thread.Sleep(2000);
            if (onConnected)
            {
                ArduinoPort.WriteLine("CONF");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Obtenemos el puerto serie que lanza el evento
            SerialPort currentSerialPort = (SerialPort)sender;

            // Leemos el dato recibido del puerto serie
            string inData = currentSerialPort.ReadLine();


            ConfiguracionInicial(inData);
            FinalesCarrera(inData);
            AngulosActuales(inData);
            Temporizadores(inData);

            if (!inData.Contains("Azimuth zero DONE") && !inData.Contains("Azimuth non zero") && !inData.Contains("Zenith zero DONE") && !inData.Contains("Zenith non zero"))
                this.campoConsola.Invoke(salidaConsola, new object[] { inData });

            //Actualizar fromulario
            Application.DoEvents();
        }

        private void Temporizadores(string inData)
        {
            if (inData.Contains("Finished"))
            {
                if (enableleft || enableright || enableup || enabledown)
                {
                    //empezar a contar
                    childref_2 = new ThreadStart(HiloTimer);
                    TimerThread = new Thread(childref_2);
                    TimerThread.Start();
                    
                }
            }else if (inData.Contains("angle exceeds"))
            {
                this.Invoke(showbutons);
                this.enableleft = false;
                this.enableright = false;
                this.enabledown = false;
                this.enableup = false;
            }
        }

        private void AngulosActuales(string inData)
        {
            if (inData.Contains("Final azimuth angle: "))
            {
                string cleanString = inData.Replace("Final azimuth angle: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoAnguloAzimut.Invoke(salidaAnguloAzimut, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Final zenith angle: "))
            {
                string cleanString = inData.Replace("Final zenith angle: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoAnguloCenit.Invoke(salidaAnguloCenit, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Azimuth angle: "))
            {
                string cleanString = inData.Replace("Azimuth angle: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoAnguloAzimut.Invoke(salidaAnguloAzimut, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Zenith angle: "))
            {
                string cleanString = inData.Replace("Zenith angle: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoAnguloCenit.Invoke(salidaAnguloCenit, new object[] { stringSeparado[1] });
                }
            }
        }

        public void FinalesCarrera(string inData)
        {
            if (inData.Contains("Azimuth zero DONE"))
            {
                this.ceroAzimut = true;
                picZeroAzimut.BackColor = Color.FromArgb(72, 239, 16);
                if (!lockMode) this.Invoke(enadisaSET, new object[] { true, true});
            }
            else if (inData.Contains("Azimuth non zero"))
            {
                this.ceroAzimut = false;
                picZeroAzimut.BackColor = Color.FromArgb(255, 255, 255);
                if (!lockMode) this.Invoke(enadisaSET, new object[] { false, true });
            }
            else if (inData.Contains("Zenith zero DONE"))
            {
                this.ceroCenit = true;
                picZeroCenit.BackColor = Color.FromArgb(72, 239, 16);
                if (!lockMode) this.Invoke(enadisaSET, new object[] { true, false });
            }
            else if (inData.Contains("Zenith non zero"))
            {
                this.ceroCenit = false;
                picZeroCenit.BackColor = Color.FromArgb(255, 255, 255);
                if (!lockMode) this.Invoke(enadisaSET, new object[] { false, false });
            }
            if (inData.Contains("STARTING INITIAL POINT"))
            {
                this.Invoke(enadisaStart, new object[] { false, false });
            }else if (inData.Contains("INITIAL POINT FINISHED"))
            {
                this.Invoke(enadisaStart, new object[] { true, false });
            }
        }

        public void AbrirPuerto(string port)
        {
            try
            {
                if (!ArduinoPort.IsOpen)
                {
                    ArduinoPort.PortName = port;
                    ArduinoPort.BaudRate = 115200;
                    ArduinoPort.DtrEnable = true;

                    // Abrimos el puerto serie
                    ArduinoPort.Open();

                    if (ArduinoPort.IsOpen)
                    {
                        this.onConnected = true;
                    }
                }

            }
            catch
            {

            }

        }

        private void ConfiguracionInicial(string inData)
        {
            if (inData.Contains("Initial manual step: "))
            {
                string cleanString = inData.Replace("Initial manual step: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoStep.Invoke(salidaSTEP, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Initial azimuth RPM: "))
            {
                string cleanString = inData.Replace("Initial azimuth RPM: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoAzimutRPM.Invoke(salidaAzimutRPM, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Initial zenith RPM: "))
            {
                string cleanString = inData.Replace("Initial zenith RPM: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoCenitRPM.Invoke(salidaCenitRPM, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Initial azimuth angle: "))
            {
                string cleanString = inData.Replace("Initial azimuth angle: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoAnguloAzimut.Invoke(salidaAnguloAzimut, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Initial zenith angle: "))
            {
                string cleanString = inData.Replace("Initial zenith angle: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.campoAnguloCenit.Invoke(salidaAnguloCenit, new object[] { stringSeparado[1] });
                }
            }
            else if (inData.Contains("Initial azimuth steps per revolution: "))
            {
                string cleanString = inData.Replace("Initial azimuth steps per revolution: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.pasosPorRevolucionAzimut = Convert.ToDouble(stringSeparado[1]);
                }
            }
            else if (inData.Contains("Initial zenith steps per revolution: "))
            {
                string cleanString = inData.Replace("Initial zenith steps per revolution: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.pasosPorRevolucionCenit = Convert.ToDouble(stringSeparado[1]);
                }
            }
            else if (inData.Contains("Initial azimuth ratio gearbox: "))
            {
                string cleanString = inData.Replace("Initial azimuth ratio gearbox: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.ratioAzimut = Convert.ToDouble(stringSeparado[1]);
                }
            }
            else if (inData.Contains("Initial zenith ratio gearbox: "))
            {
                string cleanString = inData.Replace("Initial zenith ratio gearbox: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.ratioCenit = Convert.ToDouble(stringSeparado[1]);
                }
            }
            else if (inData.Contains("Initial azimuth driver microsteps: "))
            {
                string cleanString = inData.Replace("Initial azimuth driver microsteps: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.microStepAzimut = Convert.ToDouble(stringSeparado[1]);
                }
            }
            else if (inData.Contains("Initial zenith driver microsteps: "))
            {
                string cleanString = inData.Replace("Initial zenith driver microsteps: ", "ST,");
                String[] stringSeparado = cleanString.Split(',');
                if (!string.IsNullOrEmpty(stringSeparado[1]))
                {
                    this.microStepCenit = Convert.ToDouble(stringSeparado[1]);
                }
            }
        }

        private void SalidaConsola_TextChanged(object sender, EventArgs e)
        {
            //Establecer posicion final
            campoConsola.SelectionStart = campoConsola.Text.Length;
            campoConsola.ScrollToCaret();
        }

        private void PPAL_FormClosing(object sender, FormClosingEventArgs e)
        {
            //cerrar puerto
            if (onConnected) ArduinoPort.Close();
        }

        private void BotonDER_MouseDown(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                buttonDown = true;
                Console.WriteLine("PApdd");
                ArduinoPort.WriteLine("PApdd");  // un click - un comando
                ulong cuenta = 0;
                while (buttonDown)
                {
                    Thread.Sleep(100);
                    Application.DoEvents();
                    cuenta++;                       //Contando cuanto tiempo el click esta sostenido
                    if (cuenta > 5) { break; }
                }
                if (buttonDown)
                {
                    ArduinoPort.WriteLine("PAcuo");  // click sostenido por 500ms - giro continuo
                }
            }
        }

        private void BotonDER_MouseUp(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                //enviar comando fin de pulso continuo
                buttonDown = false;
                ArduinoPort.WriteLine("PAcdt");  // detener giro continuo
            }
        }

        private void BotonIZQ_MouseDown(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                buttonDown = true;
                Console.WriteLine("PApdu");
                ArduinoPort.WriteLine("PApdu");  // un click - un comando
                ulong cuenta = 0;
                while (buttonDown)
                {
                    Thread.Sleep(100);
                    Application.DoEvents();
                    cuenta++;                       //Contando cuanto tiempo el click esta sostenido
                    if (cuenta > 5) { break; }
                }
                if (buttonDown)
                {
                    ArduinoPort.WriteLine("PAcdo");  // click sostenido por 500ms - giro continuo
                }
            }
        }

        private void BotonIZQ_MouseUp(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                //enviar comando fin de pulso continuo
                buttonDown = false;
                ArduinoPort.WriteLine("PAcit");  // detener giro continuo
            }
        }

        private void BotonUP_MouseDown(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                buttonDown = true;
                ArduinoPort.WriteLine("PCpdu");  // un click - un comando
                ulong cuenta = 0;
                while (buttonDown)
                {
                    Thread.Sleep(100);
                    Application.DoEvents();
                    cuenta++;                       //Contando cuanto tiempo el click esta sostenido
                    if (cuenta > 5) { break; }
                }
                if (buttonDown)
                {
                    ArduinoPort.WriteLine("PCcuo");  // click sostenido por 500ms - giro continuo
                }
            }
        }

        private void BotonUP_MouseUp(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                //enviar comando fin de pulso continuo
                buttonDown = false;
                ArduinoPort.WriteLine("PCcst");  // detener giro continuo
            }
        }

        private void BotonDOWN_MouseDown(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                buttonDown = true;
                ArduinoPort.WriteLine("PCpdd");  // un click - un comando
                ulong cuenta = 0;
                while (buttonDown)
                {
                    Thread.Sleep(100);
                    Application.DoEvents();
                    cuenta++;                       //Contando cuanto tiempo el click esta sostenido
                    if (cuenta > 5) { break; }
                }
                if (buttonDown)
                {
                    ArduinoPort.WriteLine("PCcdo");  // click sostenido por 500ms - giro continuo
                }
            }
        }

        private void BotonDOWN_MouseUp(object sender, MouseEventArgs e)
        {
            if (onConnected)
            {
                //enviar comando fin de pulso continuo
                buttonDown = false;
                ArduinoPort.WriteLine("PCcit");  // detener giro continuo
            }
        }

        private void CampoAzimutRPM_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                try
                {
                    enterKey = false;

                    string comandoVA = "VA";
                    if (!string.IsNullOrEmpty(campoAzimutRPM.Text))
                    {
                        double value = Convert.ToDouble(campoAzimutRPM.Text.Replace(".", ","));

                        value *= 100;

                        if (value <= 0)
                        {
                            comandoVA += "000";
                        }
                        else if (value > 0 && value < 10)
                        {
                            comandoVA += "00" + value;
                        }
                        else if (value >= 10 && value < 100)
                        {
                            comandoVA += "0" + value;
                        }
                        else if (value >= 100 && value < 1000)
                        {
                            comandoVA += value;
                        }
                        else
                        {
                            comandoVA += 999;
                        }
                        Console.WriteLine(comandoVA);
                        ArduinoPort.WriteLine(comandoVA);
                    }
                    else
                    {
                        this.campoAzimutRPM.Invoke(salidaAzimutRPM, new object[] { "0" });
                        comandoVA += "000";
                        Console.WriteLine(comandoVA);
                        ArduinoPort.WriteLine(comandoVA);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }

        private void CampoAzimutRPM_KeyPress(object sender, KeyPressEventArgs e)
        {

            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            bool IsDec = false;
            int nroDec = 0;

            for (int i = 0; i < campoAzimutRPM.Text.Length; i++)
            {
                if (campoAzimutRPM.Text[i] == '.') IsDec = true;

                if (IsDec)
                {
                    nroDec++;
                    if (nroDec >= 3)
                    {
                        e.Handled = true;
                        if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                        return;
                    }
                }
                if (i > 0 && !IsDec)
                {
                    e.Handled = true;
                    return;
                }
            }


            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else if (e.KeyChar == 46)
                e.Handled = (IsDec) ? true : false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;



        }

        private void CampoCenitRPM_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                try
                {
                    enterKey = false;

                    string comandoVC = "VC";
                    if (!string.IsNullOrEmpty(campoCenitRPM.Text))
                    {
                        double value = Convert.ToDouble(campoCenitRPM.Text.Replace(".", ","));
                        value *= 100;

                        if (value <= 0)
                        {
                            comandoVC += "000";
                        }
                        else if (value > 0 && value < 10)
                        {
                            comandoVC += "00" + value;
                        }
                        else if (value >= 10 && value < 100)
                        {
                            comandoVC += "0" + value;
                        }
                        else if (value >= 100 && value < 1000)
                        {
                            comandoVC += value;
                        }
                        else
                        {
                            comandoVC += 999;
                        }
                        Console.WriteLine(comandoVC);
                        ArduinoPort.WriteLine(comandoVC);
                    }
                    else
                    {
                        this.campoCenitRPM.Invoke(salidaCenitRPM, new object[] { "0" });
                        comandoVC += "000";
                        Console.WriteLine(comandoVC);
                        ArduinoPort.WriteLine(comandoVC);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }

        private void CampoCenitRPM_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            bool IsDec = false;
            int nroDec = 0;

            for (int i = 0; i < campoCenitRPM.Text.Length; i++)
            {
                if (campoCenitRPM.Text[i] == '.') IsDec = true;

                if (IsDec)
                {
                    nroDec++;
                    if (nroDec >= 3)
                    {
                        e.Handled = true;
                        if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                        return;
                    }
                }
                if (i > 0 && !IsDec)
                {
                    e.Handled = true;
                    return;
                }


            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else if (e.KeyChar == 46)
                e.Handled = (IsDec) ? true : false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter)
            {
                enterKey = true;
            }
        }

        private void BotonClear_Click(object sender, EventArgs e)
        {
            campoConsola.Text = String.Empty;
        }

        private void BotonInfo_Click(object sender, EventArgs e)
        {
            if (onConnected) ArduinoPort.WriteLine("CONF");
        }

        private void BotonConectar_Click(object sender, EventArgs e)
        {

            if (listaPuertos.SelectedIndex >= 0 && !onConnected)
            {
                this.botonConectar.Text = "Reconnect";
                this.campoConsola.Invoke(salidaConsola, new object[] { "Connecting...\n" });
                this.campoConsola.Invoke(salidaConsola, new object[] { "____________________________________\n" });
                AbrirPuerto(listaPuertos.SelectedItem.ToString());
            }else if (listaPuertos.SelectedIndex >= 0 && onConnected)
            {
                ArduinoPort.Close();

                childref = new ThreadStart(HiloConfigInicial);
                childThread = new Thread(childref);
                childThread.Start();

                campoConsola.Text = String.Empty;
                this.campoConsola.Invoke(salidaConsola, new object[] { "Reconnecting...\n" });
                this.campoConsola.Invoke(salidaConsola, new object[] { "____________________________________\n" });
                AbrirPuerto(listaPuertos.SelectedItem.ToString());
            }

        }

        private void BotonStartPoint_Click(object sender, EventArgs e)
        {
            if (onConnected) ArduinoPort.WriteLine("INIT");
        }

        

        private void CampoAnguloAzimut_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            bool IsDec = false;
            int nroDec = 0;

            for (int i = 0; i < campoAnguloAzimut.Text.Length; i++)
            {
                if (campoAnguloAzimut.Text[i] == '.') IsDec = true;

                if (IsDec)
                {
                    nroDec++;
                    if (nroDec >= 3)
                    {
                        e.Handled = true;
                        if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                        return;
                    }
                }
                if (i > 0 && !IsDec)
                {
                    e.Handled = true;
                    return;
                }


            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else if (e.KeyChar == 46)
                e.Handled = (IsDec) ? true : false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter)
            {
                enterKey = true;
            }
        }

        private void CampoAnguloAzimut_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                try
                {
                    enterKey = false;
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }

        private void CampoAnguloCenit_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            bool IsDec = false;
            int nroDec = 0;

            for (int i = 0; i < campoAnguloCenit.Text.Length; i++)
            {
                if (campoAnguloCenit.Text[i] == '.') IsDec = true;

                if (IsDec)
                {
                    nroDec++;
                    if (nroDec >= 3)
                    {
                        e.Handled = true;
                        if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                        return;
                    }
                }
                if (i > 0 && !IsDec)
                {
                    e.Handled = true;
                    return;
                }


            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else if (e.KeyChar == 46)
                e.Handled = (IsDec) ? true : false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter)
            {
                enterKey = true;
            }
        }

        private void CampoAnguloCenit_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                try
                {
                    enterKey = false;
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }


        #region verificartecla
        private void CampoDelayOnStepAzimut_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (campoDelayOnStep_Azi.Text.Length > 3)
            {
                e.Handled = true;
                if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                return;
            }

            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
        }

        private void CampoDelayOnStep_Zen_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (campoDelayOnStep_Zen.Text.Length > 3)
            {
                e.Handled = true;
                if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                return;
            }

            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
        }

        private void CampoDelayOnStepAzimut_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                this.enterKey = false;
            }
        }

        private void CampoDelayOnStep_Zen_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                this.enterKey = false;
            }
        }

        private void CampoStepAngle_Zen_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            bool IsDec = false;
            int nroDec = 0;

            for (int i = 0; i < campoStepAngle_Zen.Text.Length; i++)
            {
                if (campoStepAngle_Zen.Text[i] == '.') IsDec = true;

                if (IsDec)
                {
                    nroDec++;
                    if (nroDec >= 3)
                    {
                        e.Handled = true;
                        if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                        return;
                    }
                }
                if (i > 0 && !IsDec)
                {
                    e.Handled = true;
                    return;
                }


            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else if (e.KeyChar == 46)
                e.Handled = (IsDec) ? true : false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter)
            {
                enterKey = true;
            }
        }

        private void CampoStepAngle_Zen_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                this.enterKey = false;
            }
        }

        private void CampoStepAngleAzimut_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            bool IsDec = false;
            int nroDec = 0;

            for (int i = 0; i < campoStepAngle_Azi.Text.Length; i++)
            {
                if (campoStepAngle_Azi.Text[i] == '.') IsDec = true;

                if (IsDec)
                {
                    nroDec++;
                    if (nroDec >= 3)
                    {
                        e.Handled = true;
                        if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                        return;
                    }
                }
                if (i > 0 && !IsDec)
                {
                    e.Handled = true;
                    return;
                }


            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else if (e.KeyChar == 46)
                e.Handled = (IsDec) ? true : false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter)
            {
                enterKey = true;
            }
        }

        private void CampoStepAngleAzimut_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                try
                {
                    enterKey = false;
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }
        


        private void CampoStep_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (campoStep.Text.Length > 3)
            {
                e.Handled = true;
                if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
                return;
            }

            if (e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            else
                e.Handled = true;

            if ((int)e.KeyChar == (int)Keys.Enter) enterKey = true;
        }

        private void CampoStep_KeyUp(object sender, KeyEventArgs e)
        {
            if (onConnected && enterKey)
            {
                enterKey = false;
                try
                {
                    if (!string.IsNullOrEmpty(campoStep.Text))
                    {
                        long value = Int32.Parse(campoStep.Text);
                        string comando = "PG";
                        if (value <= 0)
                        {
                            comando += "00000";
                        }
                        else if (value > 0 && value < 10)
                        {
                            comando += "0000" + value;
                        }
                        else if (value >= 10 && value < 100)
                        {
                            comando += "000" + value;
                        }
                        else if (value >= 100 && value < 1000)
                        {
                            comando += "00" + value;
                        }
                        else if (value >= 1000 && value < 5000)
                        {
                            comando += "0" + value;
                        }
                        else if (value >= 5000)
                        {
                            comando += "05000";
                        }

                        ArduinoPort.WriteLine(comando);
                    }

                }
                catch
                {

                }

            }
        }
        #endregion


        private void BotonStart_Azi_left_Click(object sender, EventArgs e)
        {
            
            if (onConnected)
            {
                if (!string.IsNullOrEmpty(campoStepAngle_Azi.Text) && !string.IsNullOrEmpty(campoDelayOnStep_Azi.Text))
                {
                    HideALLbutonStep();
                    this.enableleft = true;
                    this.ciclosParos = 0;

                    this.tiempoFinal = Convert.ToUInt64(campoDelayOnStep_Azi.Text.Replace(".", ","));
                    this.anguloStepAzimut = Convert.ToDouble(campoStepAngle_Azi.Text.Replace(".",","));
                    SendStepAngle_AzimutLeft();
                    
                }
            }
        }

        private void BotonStart_Azi_right_Click(object sender, EventArgs e)
        {
            if (onConnected)
            {
                if (!string.IsNullOrEmpty(campoStepAngle_Azi.Text) && !string.IsNullOrEmpty(campoDelayOnStep_Zen.Text))
                {
                    HideALLbutonStep();
                    this.enableright = true;
                    this.ciclosParos = 0;

                    this.tiempoFinal = Convert.ToUInt64(campoDelayOnStep_Azi.Text.Replace(".", ","));
                    this.anguloStepAzimut = Convert.ToDouble(campoStepAngle_Azi.Text.Replace(".", ","));
                    SendStepAngle_AzimutRight();
                    
                }
            }
        }

        private void BotonStartZen_up_Click(object sender, EventArgs e)
        {
            if (onConnected)
            {
                if (!string.IsNullOrEmpty(campoStepAngle_Zen.Text) && !string.IsNullOrEmpty(campoDelayOnStep_Zen.Text))
                {
                    HideALLbutonStep();
                    this.enableup = true;
                    this.ciclosParos = 0;

                    this.tiempoFinal = Convert.ToUInt64(campoDelayOnStep_Zen.Text.Replace(".", ","));
                    this.anguloStepCenit = Convert.ToDouble(campoStepAngle_Zen.Text.Replace(".", ","));
                    SendStepAngle_CenitUp();
                }
            }
        }

        private void BotonStartZen_down_Click(object sender, EventArgs e)
        {
            if (onConnected)
            {
                if (!string.IsNullOrEmpty(campoStepAngle_Zen.Text) && !string.IsNullOrEmpty(campoDelayOnStep_Zen.Text))
                {
                    HideALLbutonStep();
                    this.enabledown = true;
                    this.ciclosParos = 0;

                    this.tiempoFinal = Convert.ToUInt64(campoDelayOnStep_Zen.Text.Replace(".", ","));
                    this.anguloStepCenit = Convert.ToDouble(campoStepAngle_Zen.Text.Replace(".", ","));
                    SendStepAngle_CenitDown();
                }
            }
        }


        private void Botoncancelar_Click(object sender, EventArgs e)
        {
            this.Invoke(showbutons);
            this.enableleft = false;
            this.enableright = false;
            this.enabledown = false;
            this.enableup = false;
        }

        private void BotonUnlock_Click(object sender, EventArgs e)
        {
            if (onConnected)
            {
                this.lockMode = false;
                ArduinoPort.WriteLine("UNLO");
                HideALLbutonStep();
                botonlock.BackColor = Color.Red;
                botonlock.Visible = true;
                botonUnlock.Visible = false;
                botoncancelar.Visible = false;
                botonStartPoint.Visible = false;
                if (this.ceroCenit) botonSetCenitCero.Visible = true;
                if (this.ceroAzimut) botonSetAzimutCero.Visible = true;
            }
        }

        private void Botonlock_Click(object sender, EventArgs e)
        {
            if (onConnected)
            {
                this.lockMode = true;
                ArduinoPort.WriteLine("LOCK");
                ShowALLbutonStep();
                botonUnlock.Visible = true;
                botonlock.Visible = false;
                botonSetAzimutCero.Visible = false;
                botonSetCenitCero.Visible = false;
                botonStartPoint.Visible = true;
            }
        }

        private void BotonSetAzimutCero_Click(object sender, EventArgs e)
        {
            if (onConnected)
            {
                ArduinoPort.WriteLine("REAZ");
            }
        }

        private void BotonSetCenitCero_Click(object sender, EventArgs e)
        {
            if (onConnected)
            {
                ArduinoPort.WriteLine("RECE");
            }
        }

        public void ShowALLbutonStep()
        {
            this.botonStartZen_down.Visible = true;
            this.botonStartZen_up.Visible = true;
            this.botonStart_Azi_right.Visible = true;
            this.botonStart_Azi_left.Visible = true;
            this.botoncancelar.Visible = false;
        }

        public void HideALLbutonStep()
        {
            this.botonStartZen_down.Visible = false;
            this.botonStartZen_up.Visible = false;
            this.botonStart_Azi_right.Visible = false;
            this.botonStart_Azi_left.Visible = false;
            this.botoncancelar.Visible = true;
        }

        public void SendStepAngle_AzimutLeft()
        {
            double pasos = this.anguloStepAzimut * microStepAzimut * ratioAzimut * pasosPorRevolucionAzimut / 360.0;
            pasos = Math.Floor(pasos);
            string comando = "AApdu";
            if (pasos <= 0)
            {
                comando += "000000";
            }
            else if (pasos > 0 && pasos < 10)
            {
                comando += "00000" + pasos;
            }
            else if (pasos >= 10 && pasos < 100)
            {
                comando += "0000" + pasos;
            }
            else if (pasos >= 100 && pasos < 1000)
            {
                comando += "000" + pasos;
            }
            else if (pasos >= 1000 && pasos < 10000)
            {
                comando += "00" + pasos;
            }
            else if (pasos >= 10000 && pasos < 100000)
            {
                comando += "0" + pasos;
            }
            else if (pasos >= 100000 && pasos < 1000000)
            {
                comando += pasos;
            }
            else
            {
                comando += 999999;
            }
            Console.WriteLine(comando);
            ArduinoPort.WriteLine(comando);
        }

        public void SendStepAngle_AzimutRight()
        {
            double pasos = this.anguloStepAzimut * microStepAzimut * ratioAzimut * pasosPorRevolucionAzimut / 360.0;
            pasos = Math.Floor(pasos);
            string comando = "AApdd";
            if (pasos <= 0)
            {
                comando += "000000";
            }
            else if (pasos > 0 && pasos < 10)
            {
                comando += "00000" + pasos;
            }
            else if (pasos >= 10 && pasos < 100)
            {
                comando += "0000" + pasos;
            }
            else if (pasos >= 100 && pasos < 1000)
            {
                comando += "000" + pasos;
            }
            else if (pasos >= 1000 && pasos < 10000)
            {
                comando += "00" + pasos;
            }
            else if (pasos >= 10000 && pasos < 100000)
            {
                comando += "0" + pasos;
            }
            else if (pasos >= 100000 && pasos < 1000000)
            {
                comando += pasos;
            }
            else
            {
                comando += 999999;
            }
            Console.WriteLine(comando);
            ArduinoPort.WriteLine(comando);
        }

        public void SendStepAngle_CenitUp()
        {
            double pasos = this.anguloStepCenit * microStepCenit * ratioCenit * pasosPorRevolucionCenit / 360.0;
            pasos = Math.Floor(pasos);
            string comando = "ACpdu";
            if (pasos <= 0)
            {
                comando += "000000";
            }
            else if (pasos > 0 && pasos < 10)
            {
                comando += "00000" + pasos;
            }
            else if (pasos >= 10 && pasos < 100)
            {
                comando += "0000" + pasos;
            }
            else if (pasos >= 100 && pasos < 1000)
            {
                comando += "000" + pasos;
            }
            else if (pasos >= 1000 && pasos < 10000)
            {
                comando += "00" + pasos;
            }
            else if (pasos >= 10000 && pasos < 100000)
            {
                comando += "0" + pasos;
            }
            else if (pasos >= 100000 && pasos < 1000000)
            {
                comando += pasos;
            }
            else
            {
                comando += 999999;
            }
            Console.WriteLine(comando);
            ArduinoPort.WriteLine(comando);
        }

        public void SendStepAngle_CenitDown()
        {
            double pasos = this.anguloStepCenit * microStepCenit * ratioCenit * pasosPorRevolucionCenit / 360.0;
            pasos = Math.Floor(pasos);
            string comando = "ACpdd";
            if (pasos <= 0)
            {
                comando += "000000";
            }
            else if (pasos > 0 && pasos < 10)
            {
                comando += "00000" + pasos;
            }
            else if (pasos >= 10 && pasos < 100)
            {
                comando += "0000" + pasos;
            }
            else if (pasos >= 100 && pasos < 1000)
            {
                comando += "000" + pasos;
            }
            else if (pasos >= 1000 && pasos < 10000)
            {
                comando += "00" + pasos;
            }
            else if (pasos >= 10000 && pasos < 100000)
            {
                comando += "0" + pasos;
            }
            else if (pasos >= 100000 && pasos < 1000000)
            {
                comando += pasos;
            }
            else
            {
                comando += 999999;
            }
            Console.WriteLine(comando);
            ArduinoPort.WriteLine(comando);
        }


        void EscribirConsola(string str)
        {
            campoConsola.Text += str;
        }
        void EscribirSTEP(string str)
        {
            this.campoStep.Text = str;
            this.campoStep.SelectionAlignment = HorizontalAlignment.Center;
        }
        void EscribirAzimutRPM(string str)
        {
            this.campoAzimutRPM.Text = str;
            this.campoAzimutRPM.SelectionAlignment = HorizontalAlignment.Center;
        }
        void EscribirCenitRPM(string str)
        {
            this.campoCenitRPM.Text = str;
            this.campoCenitRPM.SelectionAlignment = HorizontalAlignment.Center;
        }
        void EscribirAnguloAzimut(string str)
        {
            this.campoAnguloAzimut.Text = str;
            this.campoAnguloAzimut.SelectionAlignment = HorizontalAlignment.Center;
        }
        void EscribirAnguloCenit(string str)
        {
            this.campoAnguloCenit.Text = str;
            this.campoAnguloCenit.SelectionAlignment = HorizontalAlignment.Center;
        }
        void EscribirDelayStepAzimut(string str)
        {
            this.campoDelayOnStep_Azi.Text = str;
            this.campoDelayOnStep_Azi.SelectionAlignment = HorizontalAlignment.Center;
        }
        void EscribirDelayStepCenit(string str)
        {
            this.campoDelayOnStep_Zen.Text = str;
            this.campoDelayOnStep_Zen.SelectionAlignment = HorizontalAlignment.Center;
        }
        void GetTimesAzi()
        {
            if (listaTimesAzi.SelectedIndex >= 0)
            {
                timesSelectedAzimut = listaTimesAzi.SelectedItem.ToString();
            }

        }
        void GetTimesZen()
        {
            if (listaTimesZen.SelectedIndex >= 0)
            {
                timesSelectedCenit = listaTimesZen.SelectedItem.ToString();
            }
        }
        void BotonSET(bool state, bool azimut)
        {
            if (azimut) botonSetAzimutCero.Visible = state;
            else botonSetCenitCero.Visible = state;
        }
        void BotonStart(bool state, bool nonused)
        {
            this.botonStartPoint.Visible = state;
        }
    }
}

