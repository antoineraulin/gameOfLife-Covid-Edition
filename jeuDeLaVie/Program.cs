using EsilvGui;
using System;
using System.Text.RegularExpressions;

namespace jeuDeLaVie
{
    class Program
    {
        // Zone des variables globales
        static int[,] grille;
        static int[,] grilleTemp;
        public class Stade
        { //permet d'associer un stade comprehensible par un humain avec une couleur de la GUI.
            public int sain = 4;
            public int stade0 = 2;
            public int stade1 = 5;
            public int stade2 = 6;
            public int stade3 = 3;
            public int mort = 1;
            public int immunise = 0;

            public Stade()
            {

            }
        }

        static Stade stade = new Stade();
        static Random random = new Random();
        static Fenetre gui;

        [STAThread()] // on utilise esilvGUI, esilvGUI est basé sur winForms, et la plateforme winForms nécéssite que tout les controlleurs soient géré par un et un seul Thread, STAThread (STA : Single-Threaded Apartment) permet de forcer le code a n'utiliser qu'un seul thread a l'inverse de la commande MTAThread.
        static void Main()
        {
            object[] param = RecupererParametre();
            int x = (int)param[0];
            int y = (int)param[1];
            int versionChoice = (int)param[2];
            double tauxContamination = (double)param[3];
            double[] poidsStat = (double[])param[4];
            double tauxConfinement = (double)param[5];
            int tailleCase = (int)param[6];
            double tauxGuerison = (double)param[7];
            int nombrePatientsZero = (int)param[8];
            Console.WriteLine($" x:y : {x}:{y} | versionChoice : {versionChoice} | tauxDeContamination : {tauxContamination} | poidsStat : {string.Join(";", poidsStat)} | taux de Confinement : {tauxConfinement} | tailleCase : {tailleCase}");
            InitGrille(x, y, nombrePatientsZero);
            gui = new Fenetre(grille, tailleCase, 0, 0, "Jeu de la vie - COVID");
            AfficherMatrice(grille);
            Console.ReadKey();
            int gen = 1;
            bool stop = false;
            while (!stop)
            {

                grilleTemp = grille.Clone() as int[,]; // grilleTemp est la grille sur laquelle on va faire les modifications d'évolution avant de les appliquer sur grille pour eviter les conflits et on ne peut pas modifier grille directement puisqu'on s'appuit sur grille pour determiner l'évolution.

                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < y; j++)
                    {
                        Evoluer(i, j, tauxContamination, poidsStat, tauxGuerison);
                    }
                }
                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < y; j++)
                    {
                        grille[i, j] = grilleTemp[i, j];
                    }
                }
                //Console.WriteLine("apres");
                //AfficherMatrice(grille);
                gui.RafraichirTout();
                System.Threading.Thread.Sleep(500);
                //Console.ReadKey();
                gen++;

            }
            Console.ReadKey();

        }
        static object[] RecupererParametre()
        {
            object[] res = new object[9];
            int x, y;
            // On demande a l'utilisateur les dimensions de la grille souhaitée
            do
            {
                Console.Write("Dimensions de la grille [format : ?x? => ex : 10x12] > ");
                Regex regex = new Regex("^(.*?)x(.*?)$"); // on utilise une regular expression pour recuperer les infos d'un string selon un format particulier : ici on donne par exemple 10x12 et on obtiens 10 et 12
                Match match = regex.Match(Console.ReadLine());
                int.TryParse(match.Groups[1].Value, out x);
                int.TryParse(match.Groups[2].Value, out y);
            } while (x < 1 || y < 1);
            res[0] = x;
            res[1] = y;

            // on demande a l'utilisateur le mode de la simulation:
            int versionChoice = -1;
            do
            {
                Console.WriteLine("Menu Mode :");
                Console.WriteLine("[0]  Sans confinement");
                Console.WriteLine("[1]  Avec confinement");
                Console.Write("Votre choix > ");
                int.TryParse(Console.ReadLine(), out versionChoice);
            } while (versionChoice == -1 || versionChoice > 1);
            res[2] = versionChoice;

            // on demande a l'utilisateur s'il veut du  mode avancé.
            int advancedChoice = -1;
            do
            {
                Console.Write("Mode avancé (permet de modifier les paramètres) [N/o] > ");
                string c = Console.ReadLine().ToLower();
                if (c == "n" || c == "non" || c == "no" || c == "")
                {
                    advancedChoice = 0;
                }
                else if (c == "o" || c == "y" || c == "oui" || c == "yes")
                {
                    advancedChoice = 1;
                }
            } while (advancedChoice == -1);

            double tauxContamination = 2.5;
            double[] poidsStat = { 0.6, 0.2, 0.1, 0.02 };
            double tauxConfinement = versionChoice == 0.0 ? 0.0 : 0.8;
            int tailleCase = 0;
            double tauxGuerison = 0.3;
            int nombrePatientsZero = 1;
            if (advancedChoice == 1)
            {
                // mode avancé

                //on demande a l'utilisateur le taux de contamination:
                double tauxContaminationAdvanced;
                do
                {
                    Console.Write("Taux de contamination [par default : 2,5] > ");
                    string c = Console.ReadLine();
                    if (c != "")
                    {
                        if (!double.TryParse(c.Replace(".", ","), out tauxContaminationAdvanced))
                        {
                            tauxContaminationAdvanced = 2.5;
                        }
                    }
                    else
                    {
                        tauxContaminationAdvanced = 2.5;
                    }

                } while (tauxContaminationAdvanced == 0.0d || tauxContaminationAdvanced < 0);
                tauxContamination = tauxContaminationAdvanced;

                for (int i = 0; i < 4; i++)
                {
                    double poidsStat0;
                    do
                    {
                        Console.Write($"Poids statistique pour passer du stade {i} à {i + 1} a la prochaine génération [{poidsStat[i] * 100}] > ");
                        string c = Console.ReadLine();
                        if (c != "")
                        {
                            if (double.TryParse(c.Replace(".", ","), out poidsStat0))
                            {
                                poidsStat0 /= 100;
                            }
                            else
                            {
                                poidsStat0 = poidsStat[i];
                            }

                        }
                        else
                        {
                            poidsStat0 = poidsStat[i];
                        }
                    } while (poidsStat0 == 0.0d);
                    poidsStat[i] = poidsStat0;
                }

                if (tauxConfinement != 0.0)
                {
                    double tauxConfinementAdvanced;
                    do
                    {
                        Console.Write("Taux de confinement [80] > ");
                        string c = Console.ReadLine();
                        if (c != "")
                        {
                            if (!double.TryParse(c.Replace(".", ","), out tauxConfinementAdvanced))
                            {
                                tauxConfinementAdvanced = 0.8;
                            }
                            else
                            {
                                tauxConfinementAdvanced /= 100;
                            }
                        }
                        else
                        {
                            tauxConfinementAdvanced = 0.8;
                        }
                    } while (tauxConfinementAdvanced == 0.0d || tauxConfinementAdvanced < 0);
                    tauxConfinement = tauxConfinementAdvanced;
                }

                double tauxGuerisonAdvanced;
                do
                {
                    Console.Write("Taux de guerison [par default : 20] > ");
                    string c = Console.ReadLine();
                    if (c != "")
                    {
                        if (!double.TryParse(c.Replace(".", ","), out tauxGuerisonAdvanced))
                        {
                            tauxGuerisonAdvanced = 0.3;
                        }
                        else
                        {
                            tauxGuerisonAdvanced /= 100;
                        }
                    }
                    else
                    {
                        tauxGuerisonAdvanced = 0.3;
                    }

                } while (tauxGuerisonAdvanced == 0.0d || tauxGuerisonAdvanced < 0);
                tauxGuerison = tauxGuerisonAdvanced;

                //on demande a l'utilisateur la taille des case du GUI

                int nombrePatientsZeroAdvanced = 0;
                do
                {
                    Console.Write("Nombre de patients zero (> 0) > ");
                    int.TryParse(Console.ReadLine(), out nombrePatientsZeroAdvanced);
                } while (nombrePatientsZeroAdvanced == 0 || nombrePatientsZeroAdvanced < 0);
                nombrePatientsZero = nombrePatientsZeroAdvanced;
                do
                {
                    Console.Write("Taille des cases du GUI (> 1) > ");
                    int.TryParse(Console.ReadLine(), out tailleCase);
                } while (tailleCase < 1);
                res[5] = tailleCase;


            }
            if (tailleCase == 0)
            {
                tailleCase = 20;
            }
            res[3] = tauxContamination;
            res[4] = poidsStat;
            res[5] = tauxConfinement;
            res[6] = tailleCase;
            res[7] = tauxGuerison;
            res[8] = nombrePatientsZero;
            return res;
        }


        static void InitGrille(int x, int y, int nombrePatientsZero)
        {

            grille = new int[x, y];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    grille[i, j] = stade.sain;
                }
            }
            for (int i = 0; i < nombrePatientsZero; i++)
            {
                int individuZeroX = random.Next(x);
                int individuZeroY = random.Next(y);
                grille[individuZeroX, individuZeroY] = stade.stade0;
            }

        }

        static int Voisins(int x, int y) // renvoi le nombre de voisins vivants autour d'une coordonnée donnée sous forme d'un tableau comprenant le numéro de la population et le nombre de voisins vivants de cette populations autour de la cellule observée.
        {
            //on récupère les dimensions de la grille
            int nLignes = grilleTemp.GetUpperBound(0) + 1; // GetUpperBound => on récupère l'index du dernier élements de la dimension n (ici 0)
            int nCols = grilleTemp.GetUpperBound(1) + 1;
            int voisins = 0;
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    int ti = i; // on copie i et j avant modif dans une mémoire tampon pour pouvoir faire des modifs dessus
                    int tj = j;
                    if (ti < 0) //si ti est trop en haut de la grille on lui donne une valeur en bas puisque la grille est "ronde"
                    {
                        ti = nLignes + ti;
                    }
                    if (tj < 0) //pareil pour tj
                    {
                        tj = nCols + tj;
                    }
                    if (ti > nLignes - 1) //si ti est trop en bas de la grille on lui donne une valeur en haut
                    {
                        ti -= nLignes;
                    }
                    if (tj > nCols - 1) // pareil pour tj
                    {
                        tj -= nCols;
                    }
                    if (grilleTemp[ti, tj] == stade.sain) //si la case est saine ajoute 1 dans l'index correspondant à sa couleur
                    {
                        voisins++;
                    }
                }
            }// on retire la cellule en question du comptage;
            return voisins;
        }

        static int[][] Contaminer(int x, int y, double tauxContamination)
        {
            int[][] res = new int[8][];
            for (int i = 0; i < 8; i++)
            {
                res[i] = new int[2] { -1, -1 };

            }

            if (grille[x, y] != stade.mort && grille[x, y] != stade.immunise)
            {
                int nLignes = grille.GetUpperBound(0) + 1; // GetUpperBound => on récupère l'index du dernier élements de la dimension n (ici 0)
                int nCols = grille.GetUpperBound(1) + 1;
                int voisins = Voisins(x, y);

                int nombreDePersonneAContamine = (int)(tauxContamination / voisins);
                int n = 0;
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        int ti = i; // on copie i et j avant modif dans une mémoire tampon pour pouvoir faire des modifs dessus
                        int tj = j;
                        if (ti < 0) //si ti est trop en haut de la grille on lui donne une valeur en bas puisque la grille est "ronde"
                        {
                            ti = nLignes + ti;
                        }
                        if (tj < 0) //pareil pour tj
                        {
                            tj = nCols + tj;
                        }
                        if (ti > nLignes - 1) //si ti est trop en bas de la grille on lui donne une valeur en haut
                        {
                            ti -= nLignes;
                        }
                        if (tj > nCols - 1) // pareil pour tj
                        {
                            tj -= nCols;
                        }
                        if (grilleTemp[ti, tj] == stade.sain)
                        {
                            bool contamine = Chance2(1/tauxContamination);
                            if (contamine)
                            {
                                res[n][0] = ti;
                                res[n][1] = tj;
                                n++;
                            }
                        }
                    }
                }
            }
            return res;
        }

        static bool Chance2(double proba)
        {
           int alea;
           if(proba == 0){
               alea = -1;
           }else{
               proba = 1/ proba;
               alea = random.Next((int)proba);
           }
           return alea == 0;
        }

        static bool Chance(int probabilite)
        {
            bool res = false;
            int chance = 0;
            if (probabilite == 0)
            {
                chance = 1;
            }
            else
            {
                chance = random.Next(probabilite);
            }
            res = chance == 0;
            return res;
        }

        static void Evoluer(int x, int y, double tauxContamination, double[] poidsStats, double tauxGuerison)
        {
            if (grille[x, y] == stade.stade0 || grille[x, y] == stade.stade1 || grille[x, y] == stade.stade2 || grille[x, y] == stade.stade3) //si la cellule est malade...
            {
                int[][] contaminer = Contaminer(x, y, tauxContamination);
                int[] test = { -1, -1 };
                for (int i = 0; i < contaminer.Length; i++) //infecte les cellules autour d'elles qui doivent être contaminées (coordonnées envoyées par la fonction Contaminer)
                {
                    if (contaminer[i][0] != -1 && contaminer[i][1] != -1)
                    {
                        grilleTemp[contaminer[i][0], contaminer[i][1]] = stade.stade0;
                    }
                }
                if (grille[x, y] == stade.stade0)
                {
                    grilleTemp[x,y] = Chance2(tauxGuerison) ? stade.immunise : Chance2(poidsStats[0]) ? stade.stade1 : stade.stade0;
                    //grilleTemp[x, y] = Chance2(poidsStats[0]) ? stade.stade1 : grilleTemp[x, y] = Chance2(tauxGuerison) ? stade.immunise : stade.stade0;
                }
                else if (grille[x, y] == stade.stade1)
                {
                    grilleTemp[x,y] = Chance2(tauxGuerison) ? stade.immunise : Chance2(poidsStats[1]) ? stade.stade2 : stade.stade1;
                    //grilleTemp[x, y] = Chance2(poidsStats[1]) ? stade.stade2 : grilleTemp[x, y] = Chance2(tauxGuerison) ? stade.immunise : stade.stade1;
                }
                else if (grille[x, y] == stade.stade2)
                {
                    grilleTemp[x,y] = Chance2(tauxGuerison) ? stade.immunise : Chance2(poidsStats[2]) ? stade.stade3 : stade.stade2;
                    //grilleTemp[x, y] = Chance2(poidsStats[2]) ? stade.stade3 : grilleTemp[x, y] = Chance2(tauxGuerison) ? stade.immunise : stade.stade2;
                }
                else if (grille[x, y] == stade.stade3)
                {
                    grilleTemp[x,y] = Chance2(tauxGuerison) ? stade.immunise : Chance2(poidsStats[3]) ? stade.mort : stade.stade3;
                    //grilleTemp[x, y] = Chance2(poidsStats[3]) ? stade.mort : grilleTemp[x, y] = Chance2(tauxGuerison) ? stade.immunise : stade.stade3;
                }
            }
        }


        /*static bool ComparerGrilles(int[,] grille1, int[,] grilleC1, int[,] grilleC2)
        {
            int[] pop = PopulationTotale(grille1);
            int[] popC1 = PopulationTotale(grilleC1);
            int[] popC2 = PopulationTotale(grilleC2);
            bool res = false;
            if (pop[0] == popC1[0] && pop[1] == popC1[1] && pop[0] == popC2[0] && pop[1] == popC2[1])
            {
                res = true;
            }
            return res;
        }*/


        static void AfficherMatrice(int[,] mat)
        {
            Console.Write("\t");
            for (int i = 0; i < mat.GetLength(1); i++)
            {
                Console.Write(i + "\t");
            }
            Console.WriteLine();
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                Console.Write(i + "\t");
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(mat[i, j] + "\t");
                }
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        static void MelangerGrille()
        {
            //on récupère les dimensions de la grille
            int nLignes = grille.GetUpperBound(0) + 1; // GetUpperBound => on récupère l'index du dernier élements de la dimension n (ici 0)
            int nCols = grille.GetUpperBound(1) + 1;
            int nCase = nLignes * nCols; // nombre de cases total dans la grille

            Random random = new Random();
            for (int i = 0; i < nCase - 1; i++) // on se balade dans la grille
            {
                int j = random.Next(i, nCase); // on prend un nombre aléatoire entre la position i et la fin de notre grille [.Next(Int32, Int32) => Retourne un entier aléatoire qui se trouve dans une plage spécifiée.] ==> on déplace la case actuelle a une position aléatoire.

                // on convertit i et j en position xy pour pouvoir savoir où ils sont dans la grille
                int iLigne = i / nCols;
                int iCol = i % nCols;
                int jLigne = j / nCols;
                int jCol = j % nCols;

                // on echange les positions
                int temp = grille[iLigne, iCol]; // on place la valeur de la position i en memoire tampon
                grille[iLigne, iCol] = grille[jLigne, jCol];
                grille[jLigne, jCol] = temp;
            }
        }
    }
}