import { Component, Input } from '@angular/core';
import { ColoredTimeFrameModel, IColorModel } from '../ColoredTimeFrameModel';
import { Heap } from '../../../DataStructures/Heap';
import { ProposalResponseWrapper } from '../schedule-publisher.component';
import { IGridRenderingGeneratedModel } from './grid-rendering-generated-model';
import { IScheduleProposalUserProcessed } from '../../../models/IScheduleProposalUser';
import { UserLegendModel } from '../../../commonControls/user-legend/user-legend-model';
import { IGridLegendReadOnlyModel } from '../../common/grid-legend-read-only/grid-legend-read-only-model';

@Component({
  selector: 'app-grid-rendering-generated',
  templateUrl: './grid-rendering-generated.component.html',
  styleUrl: './grid-rendering-generated.component.scss',
})
export class GridRenderingGeneratedComponent {
  @Input() set Model(model: IGridRenderingGeneratedModel | undefined) {
    if (!model) return;
    this.ProcessResponse(model);
  }

  public GridModel: IGridLegendReadOnlyModel | undefined;

  private colorDict: { [key: string]: IColorModel } = {};

  public ColoredFramesClear: IColorModel = {
    R: 255,
    G: 255,
    B: 255,
  };
  public ColoredFramesDeficit: IColorModel = {
    R: 255,
    G: 0,
    B: 0,
  };
  public ColoredFramesBlocked: IColorModel = {
    R: 100,
    G: 100,
    B: 100,
  };

  constructor() {}

  private ProcessResponse(model: IGridRenderingGeneratedModel) {
    const response = model.responseRaw.proposalUsers
      .map<IScheduleProposalUserProcessed>((e) => {
        return {
          email: e.email,
          userId: e.userId,
          startTime: new Date(e.startTime),
          endTime: new Date(e.endTime),
        };
      })
      .sort((f, s) => f.startTime.getTime() - s.startTime.getTime());
    let i = 0;
    let j = 0;

    const coloredFrames: ColoredTimeFrameModel[] = [];

    const currentHeap = new Heap<ProposalResponseWrapper>(
      (f, s) => f.response.endTime < s.response.endTime
    );
    const currentArr: ProposalResponseWrapper[] = [];

    this.colorDict = {};

    const incrementResponse = () => {
      if (this.colorDict[response[j].email] === undefined) {
        this.colorDict[response[j].email] = {
          R: Math.random() * 255,
          G: Math.random() * 255,
          B: Math.random() * 255,
        };
      }

      const wrapper = new ProposalResponseWrapper(
        response[j],
        this.colorDict[response[j].email]
      );
      currentHeap.Add(wrapper);
      currentArr.push(wrapper);
      j++;
    };

    const incrementProposal = () => {
      while (
        currentHeap.Length() !== 0 &&
        currentHeap.Peek().response.endTime <=
          model.proposal.proposal[i].startTime
      ) {
        const wrapper = currentHeap.Pop();
        const index = currentArr.indexOf(wrapper);
        currentArr.splice(index, 1);
      }

      const startTime = model.proposal.proposal[i].startTime;
      const endTime = model.proposal.proposal[i].endTime;

      coloredFrames.push(new ColoredTimeFrameModel(startTime, endTime, []));

      // already blocked out
      const selectedExisting = model.existingCommitments.filter(
        (e) => !(endTime <= e.startTime || startTime >= e.endTime)
      );

      for (let k = 0; k < selectedExisting.length; k++) {
        coloredFrames[coloredFrames.length - 1].Color.push(
          this.ColoredFramesBlocked
        );
      }

      // employees
      for (let k = 0; k < currentArr.length; k++) {
        coloredFrames[coloredFrames.length - 1].Color.push(
          currentArr[k].colorModel
        );
      }

      // deficit
      for (
        let k = 0;
        k < model.proposal.proposal[i].employees - currentArr.length;
        k++
      ) {
        coloredFrames[coloredFrames.length - 1].Color.push(
          this.ColoredFramesDeficit
        );
      }

      // filler
      for (
        let k = 0;
        k <
        model.maxNumberOfPeople -
          model.proposal.proposal[i].employees -
          selectedExisting.length;
        k++
      ) {
        coloredFrames[coloredFrames.length - 1].Color.push(
          this.ColoredFramesClear
        );
      }

      i++;
    };

    while (i < model.proposal.proposal.length || j < response.length) {
      if (i < model.proposal.proposal.length && j < response.length) {
        if (response[j].startTime <= model.proposal.proposal[i].startTime) {
          incrementResponse();
        } else {
          incrementProposal();
        }
      } else if (i < model.proposal.proposal.length) {
        incrementProposal();
      } else {
        incrementResponse();
      }
    }

    const legendModel: UserLegendModel[] = [];

    legendModel.push({
      Name: 'Clear',
      Color: this.ColoredFramesClear,
    });

    legendModel.push({
      Name: 'Deficit',
      Color: this.ColoredFramesDeficit,
    });

    legendModel.push({
      Name: 'Blocked Out',
      Color: this.ColoredFramesBlocked,
    });

    for (let [k, v] of Object.entries(this.colorDict)) {
      legendModel.push({
        Name: k,
        Color: v,
      });
    }

    this.GridModel = {
      LegendModel: legendModel,
      ColoredFrames: coloredFrames,
    };
  }
}
