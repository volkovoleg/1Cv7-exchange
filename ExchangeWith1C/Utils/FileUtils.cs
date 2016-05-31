using System;
using System.IO;
using System.Linq;

namespace ExchangeWith1C.Utils
{
    public class FileUtils
    {
        public static string TEMP = "tmp";


        /// <summary>
        /// Перемещает файл, должен копировать с заменой. так же должен удаляться оригинал
        /// </summary>
        /// <param name="fileFrom"></param>
        /// <param name="fileTo"></param>
        public static void Move(String fileFrom, String fileTo)
        {
            if (File.Exists(fileTo) && File.Exists(fileFrom))
            {
                File.Delete(fileTo);
                File.Move(fileFrom, fileTo);
            }
            if (!File.Exists(fileTo) && File.Exists(fileFrom))
            {
                File.Move(fileFrom, fileTo);
            }
        }

        /// <summary>
        /// Проверяет, есть ли в заданной директории файл,с похожим названием
        /// </summary>
        /// <param name="directory"></param> Папка
        /// <param name="name"></param> Часть названия файла
        /// <returns></returns>
        public static bool IsExistByPartOfName(String directory, String name)
        {
            DirectoryInfo di = new DirectoryInfo(directory);
            var file = di.GetFiles().FirstOrDefault(x => x.Name.Contains(name) && !x.Name.Contains(TEMP));
            if (file != null)
            {
                return true;
            }
            else if(file==null)
            {
                return false;
            }
            return false;
        }

        public static String Get1CFileByPartOfName(String directory, String namePart)
        {
            DirectoryInfo di = new DirectoryInfo(@directory);
            var file = di.GetFiles().FirstOrDefault(x => x.Name.Contains(namePart)&&!x.Name.Contains(TEMP));
            if (file != null)
            {
                return file.Name;
            }
            else
            {
                return "";
            }
        }

        public static void Rename(String absoluteOldName, String absoluteNewName)
        {
            File.Move(@absoluteOldName, @absoluteNewName);
        }

        public static void Delete(String filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
