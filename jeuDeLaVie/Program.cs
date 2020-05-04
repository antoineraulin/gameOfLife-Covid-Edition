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
        static int gen = 1;

        static int[,] guerisonGrille;

        static int stadeDeconfinement;
        public class Stade
        { //permet d'associer un stade comprehensible par un humain avec une couleur du GUI.
            public int sain = 4;
            public int stade0 = 2;
            public int stade1 = 5;
            public int stade2 = 6;
            public int stade3 = 3;
            public int mort = 1;
            public int immunise = 0;
            public int confine = 7;
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
            dynamic[] param = RecupererParametre(); // la fonction RecupererParametre renvoit un tableau d'objet dynamique, cela peut etre des ints, des doubles, ou tout autre type possible. Cela a à la fois des avantages et des inconvéniants, ça à l'avantage de pouvoir renvoyer tout type de variable sans devoir se focaliser sur un seul type (ex: utiliser des strings pour des valeurs qui étaient des ints et des bools), mais ça à comme incovéniant majeur que le compilateur ne peut pas vérifier si on essaye pas d'attribuer une valeur booléenne à une variable int.
            int x = param[0];
            int y = param[1];
            int versionChoice = param[2];
            double tauxContamination = param[3];
            double[] poidsStat = param[4];
            double tauxConfinement = param[5];
            int tailleCase = param[6];
            double tauxGuerison = param[7];
            int nombrePatientsZero = param[8];
            stadeDeconfinement = param[9];
            InitGrille(x, y, nombrePatientsZero, tauxConfinement);
            gui = new Fenetre(grille, tailleCase, 0, 0, "Jeu de la vie - COVID");
            AfficherMatrice(grille);
            Console.ReadKey();
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
                int[] pop = Population();
                gui.changerMessage("Génération " + gen + " | sains non-immunisés : " + pop[0] + " | sains immunisés : " + pop[6] + " | stade 0 : " + pop[1] + " | stade 1 : " + pop[2] + " | stade 2 : " + pop[3] + " | stade 3 : " + pop[4] + " | morts : " + pop[5] + " | confinés : " + pop[7]);
                System.Threading.Thread.Sleep(500);
                //Console.ReadKey();

                

                gen++;

                

                if(pop[1] == 0 && pop[2] == 0 && pop[3] == 0 && pop[4] == 0){
                    //il n'y a plus de malade, on a atteint la stabilité
                    stop=true;
                }

            }
            Console.ReadKey();

        }
        static dynamic[] RecupererParametre()
        {
            //List<dynamic> res = new List<dynamic>(9);
            dynamic[] res = new dynamic[10];
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
            int stadeDeconfinement = -1;
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

                    int stadeDeconfinementAdvanced;
                    do
                    {
                        Console.Write("Stade de déconfinement > ");
                        string c = Console.ReadLine();
                        if (c != "")
                        {
                            if (!int.TryParse(c.Replace(".", ","), out stadeDeconfinementAdvanced))
                            {
                                stadeDeconfinementAdvanced = -1;
                            }
                        }
                        else
                        {
                            stadeDeconfinementAdvanced = -1;
                        }
                    } while (tauxConfinementAdvanced <= 0);
                    stadeDeconfinement = stadeDeconfinementAdvanced;
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
            res[9] = stadeDeconfinement;
            return res;
        }


        static void InitGrille(int x, int y, int nombrePatientsZero, double tauxConfinement)
        {

            grille = new int[x, y];
            guerisonGrille = new int[x, y];
            for (int i = 0; i < x * y; i++)
            {
                int iLigne = i / y;
                int iCol = i % y;
                guerisonGrille[iLigne, iCol] = 5;
            }

            int nombreConfine = (int)(x * y * tauxConfinement);

            for (int i = 0; i < nombreConfine; i++)
            {
                int iLigne = i / y;
                int iCol = i % y;
                grille[iLigne, iCol] = stade.confine;
            }

            for (int i = nombreConfine; i < x * y; i++)
            {
                int iLigne = i / y;
                int iCol = i % y;
                grille[iLigne, iCol] = stade.sain;

            }
            MelangerGrille();
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

            if (grille[x, y] != stade.mort && grille[x, y] != stade.immunise && grille[x, y] != stade.confine)
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
                            bool contamine = Chance(1 / tauxContamination);
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

        static bool Chance(double proba)
        {
            bool test = false;
            int alea = 0;
            double nombre = 0;
            if (proba == 0)
            {
                alea = -1;
            }
            else
            {
                nombre = proba * 1000;
                alea = random.Next(1000);
                test = (alea < nombre) ? true : false;
            }
            return test;
        }

        static void Evoluer(int x, int y, double tauxContamination, double[] poidsStats, double tauxGuerison)
        {
            if(gen == stadeDeconfinement && grille[x,y] == stade.confine){
                    grilleTemp[x,y] = stade.sain;
            }
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
                    
                    if (guerisonGrille[x, y] != 5)
                    {
                        // on est dans le cycle de guérison
                        guerisonGrille[x, y]--;
                        if (guerisonGrille[x, y] == 0)
                        {
                            grilleTemp[x, y] = stade.immunise;
                        }
                    }
                    else
                    {
                        if (Chance(poidsStats[0]))
                        {
                            grilleTemp[x, y] = stade.stade1;
                            guerisonGrille[x,y] += 5;
                        }
                        else
                        {
                            if (Chance(tauxGuerison))
                            {
                                guerisonGrille[x, y]--;
                            }
                        }
                    }
                }
                else if (grille[x, y] == stade.stade1)
                {
                    if (guerisonGrille[x, y] != 10)
                    {
                        // on est dans le cycle de guérison
                        guerisonGrille[x, y]--;
                        if (guerisonGrille[x, y] == 0)
                        {
                            grilleTemp[x, y] = stade.immunise;
                        }
                    }
                    else
                    {
                        if (Chance(poidsStats[1]))
                        {
                            grilleTemp[x, y] = stade.stade2;
                            guerisonGrille[x,y] += 5;
                        }
                        else
                        {
                            if (Chance(tauxGuerison))
                            {
                                guerisonGrille[x, y]--;
                            }
                        }
                    }
                    //grilleTemp[x, y] = Chance(tauxGuerison) ? stade.immunise : Chance(poidsStats[1]) ? stade.stade2 : stade.stade1;
                    //grilleTemp[x, y] = Chance(poidsStats[1]) ? stade.stade2 : Chance(tauxGuerison) ? stade.immunise : stade.stade1;
                }
                else if (grille[x, y] == stade.stade2)
                {
                    if (guerisonGrille[x, y] != 15)
                    {
                        // on est dans le cycle de guérison
                        guerisonGrille[x, y]--;
                        if (guerisonGrille[x, y] == 0)
                        {
                            grilleTemp[x, y] = stade.immunise;
                        }
                    }
                    else
                    {
                        if (Chance(poidsStats[2]))
                        {
                            grilleTemp[x, y] = stade.stade3;
                            guerisonGrille[x,y] += 5;
                        }
                        else
                        {
                            if (Chance(tauxGuerison))
                            {
                                guerisonGrille[x, y]--;
                            }
                        }
                    }
                    //grilleTemp[x, y] = Chance(tauxGuerison) ? stade.immunise : Chance(poidsStats[2]) ? stade.stade3 : stade.stade2;
                    //grilleTemp[x, y] = Chance(poidsStats[2]) ? stade.stade3 : Chance(tauxGuerison) ? stade.immunise : stade.stade2;
                }
                else if (grille[x, y] == stade.stade3)
                {
                    if (guerisonGrille[x, y] != 20)
                    {
                        // on est dans le cycle de guérison
                        guerisonGrille[x, y]--;
                        if (guerisonGrille[x, y] == 0)
                        {
                            grilleTemp[x, y] = stade.immunise;
                        }
                    }
                    else
                    {
                        if (Chance(poidsStats[3]))
                        {
                            grilleTemp[x, y] = stade.mort;
                            guerisonGrille[x,y] += 5;
                        }
                        else
                        {
                            if (Chance(tauxGuerison))
                            {
                                guerisonGrille[x, y]--;
                            }
                        }
                    }
                    //grilleTemp[x, y] = Chance(tauxGuerison) ? stade.immunise : Chance(poidsStats[3]) ? stade.mort : stade.stade3;
                    //grilleTemp[x, y] = Chance(poidsStats[3]) ? stade.mort : Chance(tauxGuerison) ? stade.immunise : stade.stade3;
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
        static int[] Population()
        {
            int[] pop = new int[8];
            foreach (int item in grille)
            {
                if (item == stade.sain)
                {
                    pop[0] += 1;
                }
                else if (item == stade.stade0)
                {
                    pop[1] += 1;
                }
                else if (item == stade.stade1)
                {
                    pop[2] += 1;
                }
                else if (item == stade.stade2)
                {
                    pop[3] += 1;
                }
                else if (item == stade.stade3)
                {
                    pop[4] += 1;
                }
                else if (item == stade.mort)
                {
                    pop[5] += 1;
                }
                else if (item == stade.immunise)
                {
                    pop[6] += 1;
                }
                else if (item == stade.confine)
                {
                    pop[7] += 1;
                }
            }
            return pop;
        }
    }
}