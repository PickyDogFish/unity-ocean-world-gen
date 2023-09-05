#define OCEANOGRAPHY_PI 3.1415926
static const float gravity = 9.81;
static const float sigmaOverRho = 0.074e-3;

float PiersonMoskowitzPeakOmega(float u){
    float nu = 0.13;
    return 2 * OCEANOGRAPHY_PI * nu * gravity / u;
}

float PiersonMoskowitz(float omega, float peakOmega)
{
	float oneOverOmega = 1 / omega;
	float peakOmegaOverOmega = peakOmega / omega;
	
	return 8.1e-3 * gravity * gravity * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega * oneOverOmega
		* exp(-1.25 * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega * peakOmegaOverOmega);
}

//g * k - deep water dispersion
//sigmaOverRho * k^3 - surface tension
//https://uwaterloo.ca/applied-mathematics/current-undergraduates/continuum-and-fluid-mechanics-students/amath-463-students/surface-gravity-waves
float Frequency(float k, float depth)
{
    float dispersion = gravity * k;
    float surfaceTension = sigmaOverRho * k * k * k;
    return  sqrt((dispersion + surfaceTension) * tanh(min(k * depth, 10)));
}