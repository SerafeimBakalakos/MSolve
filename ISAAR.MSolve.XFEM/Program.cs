﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.Matrices;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Enrichments;
using ISAAR.MSolve.XFEM.Enrichments.Functions;
using ISAAR.MSolve.XFEM.Enrichments.Items;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Geometry;
using ISAAR.MSolve.XFEM.Materials;

namespace ISAAR.MSolve.XFEM
{
    class Program
    {
        private static XNode2D[] nodeSet1 = {
            new XNode2D(0, -1.0, -1.0),
            new XNode2D(1, 1.0, -1.0),
            new XNode2D(2, 1.0, 1.0),
            new XNode2D(3, -1.0, 1.0)
        };

        private static XNode2D[] nodeSet2 = {
            new XNode2D(0, -2.0, -2.0),
            new XNode2D(1, 2.0, -2.0),
            new XNode2D(2, 2.0, 2.0),
            new XNode2D(3, -2.0, 2.0)
        };

        private static XNode2D[] nodeSet3 = {
            new XNode2D(0, 0.0, 0.0),
            new XNode2D(1, 2.0, 0.0),
            new XNode2D(2, 2.0, 2.0),
            new XNode2D(3, 0.0, 2.0)
        };

        private static XNode2D[] nodeSet4 = {
            new XNode2D(0, 0.0, 0.0),
            new XNode2D(1, 4.0, 0.0),
            new XNode2D(2, 4.0, 4.0),
            new XNode2D(3, 0.0, 4.0)
        };

        private static XNode2D[] nodeSet5 = {
            new XNode2D(0, 0.0, 0.0),
            new XNode2D(1, 4.0, 0.0),
            new XNode2D(2, 4.0, 3.0),
            new XNode2D(3, 0.0, 3.0)
        };

        private static XNode2D[] nodeSet6 = {
            new XNode2D(0, 0.2, 0.3),
            new XNode2D(1, 2.2, 1.5),
            new XNode2D(2, 3.0, 2.7),
            new XNode2D(3, 0.7, 2.0)
        };

        private static XNode2D[] nodeSet7 = {
            new XNode2D(0, 20.0, 0.0),
            new XNode2D(1, 40.0, 0.0),
            new XNode2D(2, 40.0, 20.0),
            new XNode2D(3, 20.0, 20.0)
        };

        private static void Quad4Test(XNode2D[] nodes)
        {
            double E = 1.0;
            double v = 0.25;
            double t = 1.0;
            var material = ElasticMaterial2DPlainStress.Create(E, v, t);

            var element = new Quad4(nodes, material);
            SymmetricMatrix2D<double> k = element.BuildStiffnessMatrix();
            Console.WriteLine("Quad4 stiffness matrix = ");
            Console.WriteLine(k);
        }

        private static void IsoparametricQuad4Test(XNode2D[] nodes)
        {
            double E = 1;
            double v = 0.25;
            double t = 1.0;
            var material = ElasticMaterial2DPlainStress.Create(E, v, t);

            var element = new IsoparametricQuad4(nodes, material);
            SymmetricMatrix2D<double> k = element.BuildStiffnessMatrix();
            Console.WriteLine("Isoparametric Quad4 stiffness matrix = ");
            Console.WriteLine(k);
        }

        private static void IsoparametricQuad4WithCrackTest(XNode2D[] nodes)
        {
            double E = 2e6;
            double v = 0.3;
            double t = 1.0;
            var material = ElasticMaterial2DPlainStrain.Create(E, v, t);

            ICurve2D discontinuity = new Line2D(new Point2D(30.0, 0.0), new Point2D(30.0, 20.0));
            IEnrichmentItem2D enrichmentItem = new CrackBody2D(discontinuity);

            var element = XElement2D.CreateHomogeneous(new IsoparametricQuad4(nodes), material);

            enrichmentItem.AffectElement(element);
            enrichmentItem.EnrichNodes();

            SymmetricMatrix2D<double> stiffnessStd = element.BuildStdStiffnessMatrix();
            Matrix2D<double> stiffnessStdEnr;
            SymmetricMatrix2D<double> stiffnessEnr;
            element.BuildEnrichedStiffnessMatrices(out stiffnessStdEnr, out stiffnessEnr);

            stiffnessStd.Scale(1e-6);
            stiffnessStdEnr.Scale(1e-6);
            stiffnessEnr.Scale(1e-6);

            Console.WriteLine("Quad4 standard-standard stiffness matrix = ");
            Console.WriteLine(stiffnessStd);
            Console.WriteLine("Quad4 standard-enriched stiffness matrix = ");
            Console.WriteLine(stiffnessStdEnr);
            Console.WriteLine("Quad4 enriched-enriched stiffness matrix = ");
            Console.WriteLine(stiffnessEnr);
        }

        private static void IsoparametricQuad4BimaterialTest(XNode2D[] nodes)
        {
            double E = 2e6;
            double v = 0.3;
            double t = 1.0;
            var materialLeft = ElasticMaterial2DPlainStrain.Create(E, v, t);
            var materialRight = ElasticMaterial2DPlainStrain.Create(E / 2.0, v, t);

            ICurve2D discontinuity = new Line2D(new Point2D(30.0, 0.0), new Point2D(30.0, 20.0));
            IEnrichmentItem2D enrichmentItem = new MaterialInterface2D(discontinuity);

            var element = XElement2D.CreateBimaterial(new IsoparametricQuad4(nodes), materialLeft, materialRight);

            enrichmentItem.AffectElement(element);
            enrichmentItem.EnrichNodes();

            SymmetricMatrix2D<double> stiffnessStd = element.BuildStdStiffnessMatrix();
            Matrix2D<double> stiffnessStdEnr;
            SymmetricMatrix2D<double> stiffnessEnr;
            element.BuildEnrichedStiffnessMatrices(out stiffnessStdEnr, out stiffnessEnr);

            stiffnessStd.Scale(1e-6);
            stiffnessStdEnr.Scale(1e-6);
            stiffnessEnr.Scale(1e-6);

            Console.WriteLine("Quad4 standard-standard stiffness matrix = ");
            Console.WriteLine(stiffnessStd);
            Console.WriteLine("Quad4 standard-enriched stiffness matrix = ");
            Console.WriteLine(stiffnessStdEnr);
            Console.WriteLine("Quad4 enriched-enriched stiffness matrix = ");
            Console.WriteLine(stiffnessEnr);
        }

        static void Main(string[] args)
        {
            //Quad4Test(nodeSet5);
            //IsoparametricQuad4Test(nodeSet1);

            //IsoparametricQuad4WithCrackTest(nodeSet7);
            IsoparametricQuad4BimaterialTest(nodeSet7);
        }
    }
}
