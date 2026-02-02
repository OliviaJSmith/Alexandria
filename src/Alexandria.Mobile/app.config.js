export default {
  expo: {
    name: "Alexandria",
    slug: "alexandria-mobile",
    version: "1.0.0",
    orientation: "portrait",
    icon: "./assets/icon.png",
    userInterfaceStyle: "light",
    newArchEnabled: true,
    splash: {
      image: "./assets/splash-icon.png",
      resizeMode: "contain",
      backgroundColor: "#ffffff"
    },
    ios: {
      supportsTablet: true,
      bundleIdentifier: "com.alexandria.app"
    },
    android: {
      adaptiveIcon: {
        foregroundImage: "./assets/adaptive-icon.png",
        backgroundColor: "#ffffff"
      },
      package: "com.alexandria.app"
    },
    web: {
      favicon: "./assets/favicon.png",
      bundler: "metro"
    },
    plugins: [
      "expo-web-browser"
    ],
    scheme: "alexandria",
    extra: {
      apiBaseUrl: process.env.API_BASE_URL || "http://localhost:5274/api",
      googleWebClientId: process.env.GOOGLE_WEB_CLIENT_ID || "1079278733225-2a62r2vj87e7gr0fcrlgefn51q8hbt0q.apps.googleusercontent.com",
      googleAndroidClientId: process.env.GOOGLE_ANDROID_CLIENT_ID || "",
      googleIosClientId: process.env.GOOGLE_IOS_CLIENT_ID || "",
    }
  }
};
