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

float PhillipsSpectrum(float2 k) {
    float kMag = length(k);
    if (kMag < 0.0001f) return 0.0f;

    float A = 0.2f;
    float V = 1.28f;
    float L = V * V / 9.8f;
    float l = 0.25f;
    float2 w = normalize(float2(-1.0f, 1.0f));
    float kdotw = dot(normalize(k), w);

    return A * (exp(-1.0f / ((kMag * L) * (kMag * L))) / kMag * kMag * kMag * kMag) * kdotw * kdotw * exp(-k * k * l * l);
}

float2 SamplePhillips(float2 k, float2 rand) {
    return (1.0f / sqrt(2.0f)) * rand * sqrt(PhillipsSpectrum(k));
}

//Directional spectrum https://journals.ametsoc.org/view/journals/phoc/28/3/1520-0485_1998_028_0495_ootdso_2.0.co_2.xml



float ShortWavesFade(float kLength) {
    float shortWavesFade = 1.0f;
	return exp(-shortWavesFade * shortWavesFade * kLength * kLength);
}


float NormalizationFactor(float s) {
    float s2 = s * s;
    float s3 = s2 * s;
    float s4 = s3 * s;
    if (s < 5) return -0.000564f * s4 + 0.00776f * s3 - 0.044f * s2 + 0.192f * s + 0.163f;
    else return -4.80e-08f * s4 + 1.07e-05f * s3 - 9.53e-04f * s2 + 5.90e-02f * s + 3.93e-01f;
}

float Cosine2s(float theta, float s) {
	return NormalizationFactor(s) * pow(abs(cos(0.5f * theta)), 2.0f * s);
}

float SpreadPower(float omega, float peakOmega) {
	if (omega > peakOmega)
		return 9.77f * pow(abs(omega / peakOmega), -2.5f);
	else
		return 6.97f * pow(abs(omega / peakOmega), 5.0f);
}

float DirectionSpectrum(float theta, float omega, float peakOmega) {
    float swell = 0.42;
    float angle = 22.0;
	float s = SpreadPower(omega, peakOmega) + 16 * tanh(min(omega / peakOmega, 20)) * swell * swell;
	return lerp(2.0f / 3.1415f * cos(theta) * cos(theta), Cosine2s(theta - angle, s), 1);
}