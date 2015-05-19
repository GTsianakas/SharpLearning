﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLearning.Containers.Extensions;

namespace SharpLearning.GradientBoost.GBM
{
    public interface IGBMLoss
    {
        double InitialLoss(double[] targets, bool[] inSample);

        GBMSplitInfo InitSplit(double[] targets, double[] residuals, bool[] inSample);

        double NegativeGradient(double target, double prediction);
        
        void UpdateSplitConstants(GBMSplitInfo left, GBMSplitInfo right, 
            double target, double residual);
    }

    public sealed class GBMSquaredLoss : IGBMLoss
    {
        public double InitialLoss(double[] targets, bool[] inSample)
        {
            var inSampleSum = 0.0;
            var sampleCount = 0.0;
            for (int i = 0; i < inSample.Length; i++)
            {
                if (inSample[i])
                {
                    inSampleSum += targets[i];
                    sampleCount++;
                }
            }

            return inSampleSum / sampleCount;
        }

        public GBMSplitInfo InitSplit(double[] targets, double[] residuals, bool[] inSample)
        {
            var splitInfo = GBMSplitInfo.NewEmpty();

            for (int i = 0; i < inSample.Length; i++)
            {
                if(inSample[i])
                {
                    var residual = residuals[i];
                    var residual2 = residual * residual;

                    splitInfo.Samples++;
                    splitInfo.Sum += residual;
                    splitInfo.SumOfSquares += residual2;
                }
            }

            splitInfo.Cost = splitInfo.SumOfSquares - (splitInfo.Sum * splitInfo.Sum / (double)splitInfo.Samples);
            splitInfo.BestConstant = splitInfo.Sum / (double)splitInfo.Samples;

            return splitInfo;
        }

        public double NegativeGradient(double target, double prediction)
        {
            return target - prediction;
        }

        public void UpdateSplitConstants(GBMSplitInfo left, GBMSplitInfo right, double target, double residual)
        {
            var residual2 = residual * residual;

            left.Samples++;
            left.Sum += residual;
            left.SumOfSquares += residual2;
            left.Cost = left.SumOfSquares - (left.Sum * left.Sum / (double)left.Samples);
            left.BestConstant = left.Sum / (double)left.Samples;

            right.Samples--;
            right.Sum -= residual;
            right.SumOfSquares -= residual2;
            right.Cost = right.SumOfSquares - (right.Sum * right.Sum / (double)right.Samples);
            right.BestConstant = right.Sum / (double)right.Samples;
        }
    }

    public sealed class GBMBinomialLoss : IGBMLoss
    {
        public double InitialLoss(double[] targets, bool[] inSample)
        {
            var inSampleSum = 0.0;
            var sampleCount = 0.0;
            for (int i = 0; i < inSample.Length; i++)
            {
                if (inSample[i])
                {
                    inSampleSum += targets[i];
                    sampleCount++;
                }
            }

            return Math.Log(inSampleSum / (sampleCount - inSampleSum)).NanToNum();
        }

        public GBMSplitInfo InitSplit(double[] targets, double[] residuals, bool[] inSample)
        {
            var splitInfo = GBMSplitInfo.NewEmpty();

            for (int i = 0; i < inSample.Length; i++)
            {
                if (inSample[i])
                {
                    var target = targets[i];
                    var residual = residuals[i];
                    var residual2 = residual * residual;
                    var binomial = (target - residual) * (1.0 - target + residual);

                    splitInfo.Samples++;
                    splitInfo.Sum += residual;
                    splitInfo.SumOfSquares += residual2;
                    splitInfo.BinomialSum += binomial;
                }
            }

            splitInfo.Cost = splitInfo.SumOfSquares - (splitInfo.Sum * splitInfo.Sum / (double)splitInfo.Samples);
            splitInfo.BestConstant = BinomialBestConstant(splitInfo.Sum, splitInfo.BinomialSum);

            return splitInfo;
        }

        public double NegativeGradient(double target, double prediction)
        {
            return (target - 1.0 / (1.0 + Math.Exp(-prediction))).NanToNum();
        }

        public void UpdateSplitConstants(GBMSplitInfo left, GBMSplitInfo right, double target, double residual)
        {
            var residual2 = residual * residual;
            var binomial = (target - residual) * (1.0 - target + residual);

            left.Samples++;
            left.Sum += residual;
            left.SumOfSquares += residual2;
            left.BinomialSum += binomial;
            left.Cost = left.SumOfSquares - (left.Sum * left.Sum / (double)left.Samples);
            left.BestConstant = BinomialBestConstant(left.Sum, left.BinomialSum);

            right.Samples--;
            right.Sum -= residual;
            right.SumOfSquares -= residual2;
            right.BinomialSum -= binomial;
            right.Cost = right.SumOfSquares - (right.Sum * right.Sum / (double)right.Samples);
            right.BestConstant = BinomialBestConstant(right.Sum, right.BinomialSum);
        }

        double BinomialBestConstant(double sum, double binomialSum)
        {
            if (binomialSum != 0.0)
            {
                return sum / binomialSum;
            }
            else
            {
                return 0.0;
            }
        }
    }
}
