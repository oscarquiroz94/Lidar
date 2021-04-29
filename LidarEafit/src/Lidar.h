#include "Arduino.h"

class MOTOR{
  private:
    long PASOSFROMCERO = 0L;
    long PASOSRELATIVOS = 0L;
    double DELAYHALFSTEP;  //microsegundos
    unsigned long PASOSPEREV;
    byte MICROSTEP;
    byte RATIO;
    byte PUL,DIR,ENA;
    void setDelay(double);
    double VELOCIDAD;
  public:
    MOTOR(byte,byte,byte);
    void setPasos(unsigned long,char);  
    void setAngulo(double,char);        //grados,direccion
    void setVelocidad(double);          //RPM
    double getMinVel();
    double getMaxVel();
    void setRatio(byte);
    void setPasosPerRev(unsigned long);
    void setMicroSteps(byte);
    long getPasosFromCero();
    void setPasosFromCero(long);
    long getPasosRelativos();
    void setPasosRelativos(long);
    double getDelayHalfStep();
    double getAngulo();
    double getAngulo(long);
    byte getMicroSteps();
    unsigned long getPasosPerRev();
    byte getRatio();
    double getVelocidad();
};
