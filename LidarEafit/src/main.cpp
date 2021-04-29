#include "Lidar.h"

#define FINAL_A 8
#define FINAL_C 9
#define PULSO_A 4
#define DIREC_A 3
#define ENABL_A 2
#define PULSO_C 7
#define DIREC_C 6
#define ENABL_C 5

void CheckSwitch();
void PasoAzimut();
void PasoCenit();
void AngleAzimut();
void AngleCenit();
void VelAzimut();
void VelCenit();
void PosicionInicial();
void Configuracion();
void Posicion_AzimutLeft();
void Posicion_AzimutRight();

long datos[15];
long oneStep = 1; 
bool DOWN_Continuo = false;
bool UP_Continuo = false;
bool IZQ_Continuo = false;
bool DER_Continuo = false;
bool flagSwitch_A = false;
bool flagSwitch_C = false;
bool flagConfig = false;
bool flagSettingStart = false;
bool lock = true;
bool flagInitAzimut = false;
double velMaxAzimut;
double velMaxCenit;
double velMinAzimut;
double velMinCenit;

MOTOR MAzimut(PULSO_A, DIREC_A, ENABL_A);   // Pines digitales pulso, direccion, enable del driver Azimut
MOTOR MCenit(PULSO_C, DIREC_C, ENABL_C);    // Pines digitales pulso, direccion, enable del driver Cenit

void setup() {
  Serial.begin(115200);
  Serial.setTimeout(50);
  pinMode(ENABL_A, OUTPUT); 
  pinMode(PULSO_A, OUTPUT); 
  pinMode(DIREC_A, OUTPUT);
  pinMode(ENABL_C, OUTPUT); 
  pinMode(PULSO_C, OUTPUT); 
  pinMode(DIREC_C, OUTPUT);
  pinMode(FINAL_A, INPUT_PULLUP);
  pinMode(FINAL_C, INPUT_PULLUP);
  
  digitalWrite(ENABL_C,LOW);
  digitalWrite(ENABL_A,LOW);
  
  MAzimut.setRatio(30);         // Definir ratio de reduccion gearbox Azimut
  MCenit.setRatio(80);          // Definir ratio de reduccion gearbox Cenit

  MAzimut.setPasosPerRev(200);  // Definir pasos por revolucion del motor en Azimut, 0.9° por paso --> 360/0.9
  MCenit.setPasosPerRev(200);   // Definir pasos por revolucion del motor en Cenit, 0.9° por paso --> 360/0.9

  MAzimut.setMicroSteps(32);    // Definir Micropasos por paso del Driver Azimut 1,4,8,16,32
  MCenit.setMicroSteps(32);     // Definir Micropasos por paso del Driver Cenit 1,4,8,16,32

  MAzimut.setVelocidad(1.00);   // Definir RPM ~(aprox) del gearbox. Para ratio 30, pasosrev 400, microstep 32 ---> max speed 4.00
  MCenit.setVelocidad(1.00);    // Definir RPM ~(aprox) del gearbox. Para ratio 80, pasosrev 400, microstep 32 ---> max speed 1.50

  velMaxAzimut = MAzimut.getMaxVel();
  velMaxCenit = MCenit.getMaxVel();
  velMinAzimut = MAzimut.getMinVel();
  velMinCenit = MCenit.getMinVel();
}

void loop() {
  if(UP_Continuo){
    if(MCenit.getAngulo() < 90.0 || !lock){        //Verificar angulo maximo en Cenit arriba respecto a Norte
      MCenit.setPasos(oneStep,'U');
      String angulo = String(MCenit.getAngulo(),10);
      Serial.print(F("Zenith angle: "));Serial.println(angulo);
    }else{
      Serial.println(F("Maximum zenith angle reached 90.0"));
    }
     
  }else if (DOWN_Continuo){
    if(MCenit.getAngulo() > 0.0 || !lock){        //Verificar angulo maximo en Cenit abajo respecto a Norte
      MCenit.setPasos(oneStep,'D');
      String angulo = String(MCenit.getAngulo(),10);
      Serial.print(F("Zenith angle: "));Serial.println(angulo);
    }else{
      Serial.println(F("Minimum zenith angle reached 0.0"));
    }
      
  }else if(DER_Continuo){
    if(MAzimut.getAngulo() > -180.0 || !lock){        //Verificar angulo maximo en Azimut Izquierda respecto a Norte
      MAzimut.setPasos(oneStep,'D');
      String angulo = String(MAzimut.getAngulo(),7);
      Serial.print(F("Azimuth angle: "));Serial.println(angulo);
    }else{
      Serial.println(F("Maximum azimut east angle reached -180.0"));
    }
    
  }else if(IZQ_Continuo){
    if(MAzimut.getAngulo() < 180.0 || !lock){        //Verificar angulo maximo en Azimut Derecha respecto a Norte
      MAzimut.setPasos(oneStep,'U');
      String angulo = String(MAzimut.getAngulo(),7);
      Serial.print(F("Azimuth angle: "));Serial.println(angulo);
    }else{
      Serial.println(F("Maximum azimut west angle reached 180.0"));
    }
  }
  
  if(flagConfig)CheckSwitch();
  
  delay(50);
}

void serialEvent(){
  byte index = 0;
  byte caracter;
  while(Serial.available()){
    caracter = Serial.read();
    datos[index] = caracter;
    if(caracter == '\n')break;
    index++;
    delay(2);
  }
  datos[index] = '\0';  //Finalizador de comando
  
  if(datos[0] == 'V' && datos[1] == 'A'){                     //Setear velocidad Azimut
    //VA035 --> 0.35RPM
    VelAzimut();
    memset(datos, 0, 15);
  }else if(datos[0] == 'V' && datos[1] == 'C'){               //Setear velocidad Cenit
    VelCenit();
    memset(datos, 0, 15);
  }else if(datos[0] == 'A' && datos[1] == 'A'){               //Dar angulo Azimut mediante multiples pasos
    //AApdu035000 --> 35.000 pasos
    AngleAzimut();
    memset(datos, 0, 15);
  }else if(datos[0] == 'A' && datos[1] == 'C'){               //Dar angulo Cenit mediante multiples pasos
    AngleCenit();
    memset(datos, 0, 15);
  }else if(datos[0] == 'P' && datos[1] == 'A'){               //Dar paso unitario en Azimut
    PasoAzimut();
    memset(datos, 0, 15);
  }else if(datos[0] == 'P' && datos[1] == 'C'){               //Dar paso unitario en Cenit
    PasoCenit();
    memset(datos, 0, 15);
  }else if(datos[0] == 'P' && datos[1] == 'G'){               //Setear oneStep para pasos individuales
    oneStep = (datos[2]-48)*10000 + (datos[3]-48)*1000 + (datos[4]-48)*100 + (datos[5]-48)*10 + datos[6]-48;
    Serial.print(F("Manual steps set in: "));Serial.println(oneStep);
    memset(datos, 0, 15);
  }else if(datos[0] == 'C' && datos[1] == 'O' && datos[2] == 'N' && datos[3] == 'F'){
    Configuracion();
    memset(datos, 0, 15);
  }else if(datos[0] == 'I' && datos[1] == 'N' && datos[2] == 'I' && datos[3] == 'T'){
    PosicionInicial();
    memset(datos, 0, 15);
  }else if(datos[0] == 'U' && datos[1] == 'N' && datos[2] == 'L' && datos[3] == 'O'){
    lock = false;
    memset(datos, 0, 15);
  }else if(datos[0] == 'L' && datos[1] == 'O' && datos[2] == 'C' && datos[3] == 'K'){
    lock = true;
    memset(datos, 0, 15);
  }else if(datos[0] == 'R' && datos[1] == 'E' && datos[2] == 'A' && datos[3] == 'Z'){
    MAzimut.setPasosFromCero(0L);
    MAzimut.setPasosRelativos(0L);
    String angulo = String(MAzimut.getAngulo(),7);
    Serial.print(F("Azimuth angle: "));Serial.println(angulo);
    memset(datos, 0, 15);
  }else if(datos[0] == 'R' && datos[1] == 'E' && datos[2] == 'C' && datos[3] == 'E'){
    MCenit.setPasosFromCero(0L);
    MCenit.setPasosRelativos(0L);
    String angulo = String(MCenit.getAngulo(),10);
    Serial.print(F("Zenith angle: "));Serial.println(angulo);
    memset(datos, 0, 15);
  }
}

void CheckSwitch(){
  if(!digitalRead(FINAL_A) && !flagSwitch_A){
    flagSwitch_A = true;
    Serial.println(F("Azimuth zero DONE"));
    delay(100);
  }else if(digitalRead(FINAL_A) && flagSwitch_A){
    flagSwitch_A = false;
    Serial.println(F("Azimuth non zero"));
    delay(100);
  }
  
  if(!digitalRead(FINAL_C) && !flagSwitch_C){
    flagSwitch_C = true;
    Serial.println(F("Zenith zero DONE"));
    delay(100);
  }else if(digitalRead(FINAL_C) && flagSwitch_C){
    flagSwitch_C = false;
    Serial.println(F("Zenith non zero"));
    delay(100);
  }
}

void PasoAzimut(){
  if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'u'){            //Paso discreto UP/Izquierda
    if(flagInitAzimut){
      flagInitAzimut = false;
      Posicion_AzimutLeft();  //Girar en azimut izquierda hasta que se active final de carrera
    }else{
      if(MAzimut.getAngulo() < 180.0 || !lock){        //Verificar angulo maximo en Azimut Derecha respecto a Norte
        MAzimut.setPasos(oneStep,'U');
        String angulo = String(MAzimut.getAngulo(),7);
        Serial.print(F("Azimuth angle: "));Serial.println(angulo);
      }else{
        Serial.println(F("Maximum azimut west angle reached 180.0"));
      }
    }
    
  }else if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'd'){      //Paso discreto DOWN/Derecha
    if(flagInitAzimut){
      flagInitAzimut = false;
      Posicion_AzimutRight();  //Girar en azimut derecha hasta que se active final de carrera
    }else{
      if(MAzimut.getAngulo() > -180.0 || !lock){        //Verificar angulo maximo en Azimut Izquierda respecto a Norte
        MAzimut.setPasos(oneStep,'D');
        String angulo = String(MAzimut.getAngulo(),7);
        Serial.print(F("Azimuth angle: "));Serial.println(angulo);
      }else{
        Serial.println(F("Maximum azimut east angle reached -180.0"));
      }
    }
    
  }else if(datos[2] == 'c' && datos[3] == 'u' && datos[4] == 'o'){      //Paso continuo DERE ON
    //activar bandera en loop
    DER_Continuo = true;
    
  }else if(datos[2] == 'c' && datos[3] == 'd' && datos[4] == 'o'){      //Paso continuo IZQ ON
    //activar bandera en loop
    IZQ_Continuo = true;
    
  }else if(datos[2] == 'c' && datos[3] == 'd' && datos[4] == 't'){      //Paso continuo DERE STOP 
    //desactivar bandera en loop
    DER_Continuo = false;
    
  }else if(datos[2] == 'c' && datos[3] == 'i' && datos[4] == 't'){      //Paso continuo IZQ STOP
    //desactivar bandera en loop
    IZQ_Continuo = false;
    
  }
}

void PasoCenit(){
  if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'u'){            //Paso discreto UP/Derecha
    if(MCenit.getAngulo() < 90.0 || !lock){        //Verificar angulo maximo en Cenit arriba respecto a Norte
      MCenit.setPasos(oneStep,'U');
      String angulo = String(MCenit.getAngulo(),10);
      Serial.print(F("Zenith angle: "));Serial.println(angulo);
    }else{
      Serial.println(F("Maximum zenith angle reached 90.0"));
    }
    
  }else if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'd'){      //Paso discreto DOWN/Izquierda
    if(MCenit.getAngulo() > 0.0 || !lock){        //Verificar angulo maximo en Cenit abajo respecto a Norte
      MCenit.setPasos(oneStep,'D');
      String angulo = String(MCenit.getAngulo(),10);
      Serial.print(F("Zenith angle: "));Serial.println(angulo);
    }else{
      Serial.println(F("Minimum zenith angle reached 0.0"));
    }
    
  }else if(datos[2] == 'c' && datos[3] == 'u' && datos[4] == 'o'){      //Paso continuo UP ON
    //activar bandera en loop
    UP_Continuo = true;
    
  }else if(datos[2] == 'c' && datos[3] == 'd' && datos[4] == 'o'){      //Paso continuo DOWN ON
    //activar bandera en loop
    DOWN_Continuo = true;
    
  }else if(datos[2] == 'c' && datos[3] == 's' && datos[4] == 't'){      //Paso continuo UP STOP
    //desactivar bandera en loop
    UP_Continuo = false;
    
  }else if(datos[2] == 'c' && datos[3] == 'i' && datos[4] == 't'){      //Paso continuo DOWN STOP
    //desactivar bandera en loop
    DOWN_Continuo = false;
  }
}

void AngleAzimut(){
  unsigned long pasos = (unsigned long)((datos[5]-48)*100000 + (datos[6]-48)*10000 + (datos[7]-48)*1000 + (datos[8]-48)*100 + (datos[9]-48)*10 + (datos[10]-48)*1);
  if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'u'){            //Pasos UP/Derecha
    if(MAzimut.getAngulo(pasos) + MAzimut.getAngulo() <= 180.0){        //Verificar angulo maximo en Azimut Derecha respecto a Norte como la suma de paso actual y el requerido
      //String anguloP = String(MAzimut.getAngulo(pasos),7);
      //String stepsT = String(MAzimut.getPasosFromCero() - MAzimut.getPasosRelativos());
      //String pasosC = String(MAzimut.getPasosFromCero());
      //String angulo = String(MAzimut.getAngulo(),7);
      Serial.println("");
      Serial.print(F("--- Started --- "));Serial.print(F("requested azimuth angle: "));Serial.println(MAzimut.getAngulo(pasos),7);delay(50);
      MAzimut.setPasos(pasos,'U');
      Serial.print(F("Steps taken in azimuth: "));Serial.println(MAzimut.getPasosFromCero() - MAzimut.getPasosRelativos());delay(50);
      Serial.print(F("Total azimuth steps: "));Serial.println(MAzimut.getPasosFromCero());delay(50);
      Serial.print(F("Final azimuth angle: "));Serial.println(MAzimut.getAngulo(),7);delay(50);
      Serial.print(F("--- Finished"));Serial.println("");delay(50);
    }else{
      Serial.println("");
      Serial.println(F("The requested azimuth angle exceeds the allowed: 180.0"));
    }
    
  }else if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'd'){      //Pasos DOWN/Izquierda
    if(MAzimut.getAngulo(-pasos) + MAzimut.getAngulo() >= -180.0){        //Verificar angulo maximo en Azimut Izquierda respecto a Norte como la suma de paso actual y el requerido
      //String anguloP = String(MAzimut.getAngulo(-pasos),7);
      //String stepsT = String(MAzimut.getPasosFromCero() - MAzimut.getPasosRelativos());
      //String pasosC = String(MAzimut.getPasosFromCero());
      //String angulo = String(MAzimut.getAngulo(),7);
      Serial.println("");
      Serial.print(F("--- Started --- "));Serial.print(F("requested azimuth angle: "));Serial.println(MAzimut.getAngulo(-pasos),7);delay(50);
      MAzimut.setPasos(pasos,'D');
      Serial.print(F("Steps taken in azimuth: "));Serial.println(MAzimut.getPasosFromCero() - MAzimut.getPasosRelativos());delay(50);
      Serial.print(F("Total azimuth steps: "));Serial.println(MAzimut.getPasosFromCero());delay(50);
      Serial.print(F("Final azimuth angle: "));Serial.println(MAzimut.getAngulo(),7);delay(50);
      Serial.print(F("--- Finished"));Serial.println("");delay(50);
    }else{
      Serial.println("");
      Serial.println(F("The requested azimuth angle exceeds the allowed: -180.0"));
    }
  }
}

void AngleCenit(){
  unsigned long pasos = (unsigned long)((datos[5]-48)*100000 + (datos[6]-48)*10000 + (datos[7]-48)*1000 + (datos[8]-48)*100 + (datos[9]-48)*10 + (datos[10]-48)*1);
  if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'u'){            //Pasos UP/Derecha
    if(MCenit.getAngulo(pasos) + MCenit.getAngulo() <= 90.0){        
      //String anguloP = String(MCenit.getAngulo(pasos),10);
      //String stepsT = String(MCenit.getPasosFromCero() - MCenit.getPasosRelativos());
      //String pasosC = String(MCenit.getPasosFromCero());
      //String angulo = String(MCenit.getAngulo(),10);
      Serial.println("");
      Serial.print(F("--- Started --- "));Serial.print(F("requested zenith angle: "));Serial.println(MCenit.getAngulo(pasos),10);delay(50);
      MCenit.setPasos(pasos,'U');
      Serial.print(F("Steps taken in zenith: "));Serial.println(MCenit.getPasosFromCero() - MCenit.getPasosRelativos());delay(50);
      Serial.print(F("Total zenith steps: "));Serial.println(MCenit.getPasosFromCero());delay(50);
      Serial.print(F("Final zenith angle: "));Serial.println(MCenit.getAngulo(),10);delay(50);
      Serial.println(F("--- Finished"));delay(50);
    }else{
      Serial.println("");
      Serial.println(F("The requested zenith angle exceeds the allowed: 90.0"));
    }
    
  }else if(datos[2] == 'p' && datos[3] == 'd' && datos[4] == 'd'){      //Pasos DOWN/Izquierda
    if(MCenit.getAngulo(-pasos) + MCenit.getAngulo() >= 0.0){        
      //String anguloP = String(MCenit.getAngulo(-pasos),10);
      //String stepsT = String(MCenit.getPasosFromCero() - MCenit.getPasosRelativos());
      //String pasosC = String(MCenit.getPasosFromCero());
      //String angulo = String(MCenit.getAngulo(),10);
      Serial.println("");
      Serial.print(F("--- Started --- "));Serial.print(F("requested zenith angle: "));Serial.println(MCenit.getAngulo(-pasos),10);delay(50);
      MCenit.setPasos(pasos,'D');
      Serial.print(F("Steps taken in zenith: "));Serial.println(MCenit.getPasosFromCero() - MCenit.getPasosRelativos());delay(50);
      Serial.print(F("Total zenith steps: "));Serial.println(MCenit.getPasosFromCero());delay(50);
      Serial.print(F("Final zenith angle: "));Serial.println(MCenit.getAngulo(),10);delay(50);
      Serial.println(F("--- Finished"));delay(50);
    }else{
      Serial.println("");
      Serial.println(F("The requested zenith angle exceeds the allowed: 0.0"));
    }
  }
}

void VelAzimut(){
  double vel = (double)((datos[2]-48)*100 + (datos[3]-48)*10 + (datos[4]-48)*1) / 100.0;  
  if(vel < velMaxAzimut && vel > velMinAzimut){
    MAzimut.setVelocidad(vel);
    String vels = String(vel);
    Serial.print(F("New azimuth speed "));Serial.print(vels);Serial.println(F(" RPM"));
  }else if(vel >= velMaxAzimut){
    MAzimut.setVelocidad(velMaxAzimut);
    String velmaxs = String(velMaxAzimut);
    Serial.print(F("Maximum azimuth speed reached "));Serial.print(velmaxs);Serial.println(F(" RPM"));
  }else if(vel <= velMinAzimut){
    MAzimut.setVelocidad(velMinAzimut);
    String velmins = String(velMinAzimut);
    Serial.print(F("Minimum azimuth speed reached "));Serial.print(velmins);Serial.println(F(" RPM"));
  }
}

void VelCenit(){
  double vel = (double)((datos[2]-48)*100 + (datos[3]-48)*10 + (datos[4]-48)*1) / 100.0;
  if(vel < velMaxCenit && vel > velMinCenit){
    MCenit.setVelocidad(vel);
    String vels = String(vel);
    Serial.print(F("New zenith speed "));Serial.print(vels);Serial.println(F(" RPM"));
  }else if(vel >= velMaxCenit){
    MCenit.setVelocidad(velMaxCenit);
    String velmaxs = String(velMaxCenit);
    Serial.print(F("Maximum zenith speed reached "));Serial.print(velmaxs);Serial.println(F(" RPM"));
  }else if(vel <= velMinCenit){
    MCenit.setVelocidad(velMinCenit);
    String velmins = String(velMinCenit);
    Serial.print(F("Minimum zenith speed reached "));Serial.print(velmins);Serial.println(F(" RPM"));
  }
}

void PosicionInicial(){
  Serial.println();
  Serial.println(F("-------- STARTING INITIAL POINT ----------"));
  Serial.println(F("Press azimuth limit switch..."));
  while(digitalRead(FINAL_A)){}
  Serial.println(F("Azimuth zero DONE"));
  delay(2000);
  Serial.println(F("Press zenith limit switch... "));
  delay(100);
  while(digitalRead(FINAL_C)){}
  Serial.println(F("Zenith zero DONE"));
  delay(1000);
  if(digitalRead(FINAL_C))Serial.println(F("Zenith non zero"));  //Apagar led en pantalla
  delay(100);
  if(digitalRead(FINAL_A))Serial.println(F("Azimuth non zero"));
  delay(1000);
  Serial.println(F("Turning down on zenith..."));
  delay(100);

  //Descender en cenit
  while(digitalRead(FINAL_C)){MCenit.setPasos(1,'D');}
  MCenit.setPasosFromCero(0L);
  MCenit.setPasosRelativos(0L);
  String angulo = String(MCenit.getAngulo(),10);
  Serial.print(F("Zenith angle: "));Serial.println(angulo);
  delay(100);
  Serial.println(F("Zenith zero DONE"));
  delay(100);

  //Preguntar por angulo y girar en azimuth, no necesariamente se conoce el angulo actual (reinicio)
  Serial.println(F("Press desired direction button for Azimuth..."));
  delay(100);

  //Activar bandera y en serialevent verificar esta bandera en los comandos de llegada, ejecutar rutina segun corresponda
  flagInitAzimut = true;

  //CleanBuffer();
}

void Configuracion(){
  String steps = String(oneStep);
  String stperevA = String(MAzimut.getPasosPerRev());
  String stperevC = String(MCenit.getPasosPerRev());
  String ratioA = String(MAzimut.getRatio());
  String ratioC = String(MCenit.getRatio());
  String microsA = String(MAzimut.getMicroSteps());
  String microsC = String(MCenit.getMicroSteps());
  String rpmC = String(MCenit.getVelocidad(),2);
  String rpmA = String(MAzimut.getVelocidad(),2);
  String azimut = String(MAzimut.getAngulo(),7);
  String cenit = String(MCenit.getAngulo(),10);
  Serial.print(F("Initial manual step: "));Serial.println(steps);
  delay(100);
  Serial.print(F("Initial azimuth steps per revolution: "));Serial.println(stperevA);
  delay(100);
  Serial.print(F("Initial zenith steps per revolution: "));Serial.println(stperevC);
  delay(100);
  Serial.print(F("Initial azimuth ratio gearbox: "));Serial.println(ratioA);
  delay(100);
  Serial.print(F("Initial zenith ratio gearbox: "));Serial.println(ratioC);
  delay(100);
  Serial.print(F("Initial azimuth driver microsteps: "));Serial.println(microsA);
  delay(100);
  Serial.print(F("Initial zenith driver microsteps: "));Serial.println(microsC);
  delay(100);
  Serial.print(F("Initial azimuth RPM: "));Serial.println(rpmA);
  delay(100);
  Serial.print(F("Initial zenith RPM: "));Serial.println(rpmC);
  delay(100);
  Serial.print(F("Initial azimuth angle: "));Serial.println(azimut);
  delay(100);
  Serial.print(F("Initial zenith angle: "));Serial.println(cenit);
  delay(100);
  if(!digitalRead(FINAL_A))Serial.println(F("Azimuth zero DONE"));
  delay(100);
  if(!digitalRead(FINAL_C))Serial.println(F("Zenith zero DONE"));
  delay(100);
  flagConfig = true;
}

void Posicion_AzimutLeft(){
  Serial.println(F("Turning left on Azimuth..."));
  delay(1000);
  while(digitalRead(FINAL_A)){MAzimut.setPasos(1,'U');}
  MAzimut.setPasosFromCero(0L);
  MAzimut.setPasosRelativos(0L);
  String angulo = String(MAzimut.getAngulo(),7);
  Serial.print(F("Azimuth angle: "));Serial.println(angulo);
  delay(100);
  Serial.println(F("Azimuth zero DONE"));
  delay(100);
  Serial.println(F("-------- INITIAL POINT FINISHED ----------"));
  delay(100);
}

void Posicion_AzimutRight(){
  Serial.println(F("Turning right on Azimuth..."));
  delay(1000);
  while(digitalRead(FINAL_A)){MAzimut.setPasos(1,'D');}
  MAzimut.setPasosFromCero(0L);
  MAzimut.setPasosRelativos(0L);
  String angulo = String(MAzimut.getAngulo(),7);
  Serial.print(F("Azimuth angle: "));Serial.println(angulo);
  delay(100);
  Serial.println(F("Azimuth zero DONE"));
  delay(100);
  Serial.println(F("-------- INITIAL POINT FINISHED ----------"));
  delay(100);
}