#include "Lidar.h"

MOTOR::MOTOR(byte pul, byte dir, byte ena){
  PUL = pul;
  DIR = dir;
  ENA = ena;
}

void MOTOR::setPasos(unsigned long pasos, char direccion){
  PASOSRELATIVOS = PASOSFROMCERO;
  switch (direccion){
    case 'U':
      digitalWrite(DIR,LOW);
      digitalWrite(ENA,HIGH);
      for(unsigned long i=0L; i<pasos; i++){
        digitalWrite(PUL,HIGH);
        delayMicroseconds((unsigned long)DELAYHALFSTEP);
        digitalWrite(PUL,LOW);
        delayMicroseconds((unsigned long)DELAYHALFSTEP);
        PASOSFROMCERO++;
      }
      digitalWrite(ENA,LOW);
      break;
    case 'D':
      digitalWrite(DIR,HIGH);
      digitalWrite(ENA,HIGH);
      for(unsigned long i=0L; i<pasos; i++){
        digitalWrite(PUL,HIGH);
        delayMicroseconds((unsigned long)DELAYHALFSTEP);
        digitalWrite(PUL,LOW);
        delayMicroseconds((unsigned long)DELAYHALFSTEP);
        PASOSFROMCERO--;
      }
      digitalWrite(ENA,LOW);
      break;
    default:
      break;
  }

}

void MOTOR::setAngulo(double ang, char direccion){
  double pasosAng = ang * MICROSTEP * RATIO * PASOSPEREV / 360.0;
  setPasos((unsigned long)pasosAng,direccion);
}

double MOTOR::getAngulo(){
  return 360.0 * PASOSFROMCERO / (PASOSPEREV * MICROSTEP * RATIO);
}
double MOTOR::getAngulo(long pasos){
  return 360.0 * pasos / (PASOSPEREV * MICROSTEP * RATIO);
}

void MOTOR::setVelocidad(double vel){
  VELOCIDAD = vel;
  double delayt = 30.0 * 1000.0 * 1000.0  / ((double)RATIO * (double)PASOSPEREV * (double)MICROSTEP * vel);
  setDelay(delayt);
}

double MOTOR::getMinVel(){
  double maxHalfDelayMotor = 2000.0;
  return 30.0 * 1000.0 * 1000.0  / ((double)RATIO * (double)PASOSPEREV * (double)MICROSTEP * maxHalfDelayMotor);
}

double MOTOR::getMaxVel(){
  double minHalfDelayMotor = 20.0;
  return 30.0 * 1000.0 * 1000.0  / ((double)RATIO * (double)PASOSPEREV * (double)MICROSTEP * minHalfDelayMotor); 
}

double MOTOR::getVelocidad(){
  return VELOCIDAD;
}

void MOTOR::setDelay(double tiempo){
  DELAYHALFSTEP = tiempo; 
}

long MOTOR::getPasosFromCero(){
  return PASOSFROMCERO;
}

long MOTOR::getPasosRelativos(){
  return PASOSRELATIVOS;
}

void MOTOR::setPasosFromCero(long val){
  PASOSFROMCERO = val;
}

void MOTOR::setPasosRelativos(long val){
  PASOSRELATIVOS = val;
}

void MOTOR::setRatio(byte ratio){
  RATIO = ratio;
}
byte MOTOR::getRatio(){
  return RATIO;
}

void MOTOR::setPasosPerRev(unsigned long ppr){
  PASOSPEREV = ppr;
}
unsigned long MOTOR::getPasosPerRev(){
  return PASOSPEREV;
}

void MOTOR::setMicroSteps(byte mcstep){
  MICROSTEP = mcstep;
}
byte MOTOR::getMicroSteps(){
  return MICROSTEP;
}

double MOTOR::getDelayHalfStep(){
  return DELAYHALFSTEP;
}
