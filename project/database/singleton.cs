﻿namespace csgo {
    public abstract class Singleton<T>
    where T : Singleton<T>, new() {
        private static T _instance = new T( );
        public static T Instance( ) {
            return _instance;
        }
    }
}