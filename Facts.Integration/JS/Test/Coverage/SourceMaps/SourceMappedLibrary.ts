module SourceMaps.Library {
    export class MathUtil {
        public Add(first: number, second: number) {
            return first + second;
        }

        public IsEven(num: number) {
            if (num % 2 == 0) {
                return true;
            }

            return false;
        }

        public static StaticAdd(first: number, second: number) {
            return first + second;
        }

        public static StaticIsEven(num: number) {
            if (num % 2 == 0) {
                return true;
            }

            return false;
        }
    }
} 