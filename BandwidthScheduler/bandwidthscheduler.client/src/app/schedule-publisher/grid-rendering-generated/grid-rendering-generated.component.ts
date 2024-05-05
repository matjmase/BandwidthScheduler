import { Component, Input } from '@angular/core';
import { ColoredTimeFrameModel, IColorModel } from '../ColoredTimeFrameModel';
import { Heap } from '../../DataStructures/Heap';
import { ProposalResponseWrapper } from '../schedule-publisher.component';
import { IGridRenderingGeneratedModel } from './grid-rendering-generated-model';
import { IScheduleProposalUserProcessed } from '../../models/IScheduleProposalUser';

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

  public ColoredFrames: ColoredTimeFrameModel[] = [];
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

  public ColorDict: { [key: string]: IColorModel } = {};

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

    this.ColoredFrames = [];

    const currentHeap = new Heap<ProposalResponseWrapper>(
      (f, s) => f.response.endTime < s.response.endTime
    );
    const currentArr: ProposalResponseWrapper[] = [];

    const incrementResponse = () => {
      if (this.ColorDict[response[j].email] === undefined) {
        this.ColorDict[response[j].email] = {
          R: Math.random() * 255,
          G: Math.random() * 255,
          B: Math.random() * 255,
        };
      }

      const wrapper = new ProposalResponseWrapper(
        response[j],
        this.ColorDict[response[j].email]
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
        console.log('pop!');
        console.log(currentArr[index]);
        currentArr.splice(index, 1);
      }

      this.ColoredFrames.push(
        new ColoredTimeFrameModel(
          model.proposal.proposal[i].startTime,
          model.proposal.proposal[i].endTime,
          []
        )
      );

      // employees
      for (let k = 0; k < currentArr.length; k++) {
        this.ColoredFrames[this.ColoredFrames.length - 1].Color.push(
          currentArr[k].colorModel
        );
      }

      // deficit
      for (
        let k = 0;
        k < model.proposal.proposal[i].employees - currentArr.length;
        k++
      ) {
        this.ColoredFrames[this.ColoredFrames.length - 1].Color.push(
          this.ColoredFramesDeficit
        );
      }

      // filler
      for (
        let k = 0;
        k < model.maxNumberOfPeople - model.proposal.proposal[i].employees;
        k++
      ) {
        this.ColoredFrames[this.ColoredFrames.length - 1].Color.push(
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
  }
}
