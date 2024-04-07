import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { TimeFrameModel, TriStateButton } from './TimeFrameModel';
import { IScheduleProposalRequest } from '../models/IScheduleProposalRequest';
import { BackendConnectService } from '../services/backend-connect.service';
import { ColoredTimeFrameModel, IColorModel } from './ColoredTimeFrameModel';
import {
  IScheduleProposalResponse,
  IScheduleProposalResponseProcessed,
} from '../models/IScheduleProposalResponse';
import { Heap } from '../DataStructures/Heap';

@Component({
  selector: 'app-schedule-publisher',
  templateUrl: './schedule-publisher.component.html',
  styleUrl: './schedule-publisher.component.scss',
})
export class SchedulePublisherComponent {
  private timeSpan = 30;
  private totalMinutes = 24 * 60;

  private totalBlock = this.totalMinutes / this.timeSpan;

  private currentDate: Date | undefined;

  public MaxNumberOfPeople: number = 10;
  public TimeFrames: TimeFrameModel[] = [];

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

  constructor(private backend: BackendConnectService) {}

  public GetDateTransformed(increment: number): Date {
    const cloneDate = new Date(this.currentDate!);

    cloneDate.setMinutes(this.timeSpan * increment);

    return cloneDate;
  }

  public Submit(form: NgForm) {
    this.currentDate = form.value.date as Date;
    this.MaxNumberOfPeople = form.value.maxEmployees as number;

    this.TimeFrames = [];

    for (let i = 0; i < this.totalBlock; i++) {
      const startTime = this.GetDateTransformed(i);
      this.TimeFrames.push(
        new TimeFrameModel(
          startTime,
          this.GetDateTransformed(i + 1),
          new Array(this.MaxNumberOfPeople).fill(false)
        )
      );
    }
  }

  public ClearSelection(): void {
    for (let timeFrame of this.TimeFrames) {
      for (let i = 0; i < timeFrame.Level.length; i++) {
        timeFrame.Level[i] = TriStateButton.NotSelected;
      }
    }
  }

  public GenerateProposal(): void {
    const proposal: IScheduleProposalRequest[] = [];

    for (let timeFrame of this.TimeFrames) {
      let finalLevel = 0;

      for (var i = timeFrame.Level.length - 1; i >= 0; i--) {
        if (timeFrame.Level[i] === TriStateButton.Selected) {
          finalLevel = i + 1;
          break;
        }
      }

      proposal.push({
        startTime: timeFrame.StartTime,
        endTime: timeFrame.EndTime,
        employees: finalLevel,
      });
    }

    this.backend.Publish.RequestScheduleTimes(proposal).subscribe({
      next: (resp) => {
        console.log(resp);
        this.ProcessResponse(proposal, resp);
      },
    });
  }

  private ProcessResponse(
    proposal: IScheduleProposalRequest[],
    responseRaw: IScheduleProposalResponse[]
  ) {
    const response = responseRaw
      .map<IScheduleProposalResponseProcessed>((e) => {
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

    console.log(proposal);

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
        currentHeap.Peek().response.endTime <= proposal[i].startTime
      ) {
        const wrapper = currentHeap.Pop();
        const index = currentArr.indexOf(wrapper);
        console.log('pop!');
        console.log(currentArr[index]);
        currentArr.splice(index, 1);
      }

      this.ColoredFrames.push(
        new ColoredTimeFrameModel(
          proposal[i].startTime,
          proposal[i].endTime,
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
      for (let k = 0; k < proposal[i].employees - currentArr.length; k++) {
        this.ColoredFrames[this.ColoredFrames.length - 1].Color.push(
          this.ColoredFramesDeficit
        );
      }

      // filler
      for (let k = 0; k < this.MaxNumberOfPeople - proposal[i].employees; k++) {
        this.ColoredFrames[this.ColoredFrames.length - 1].Color.push(
          this.ColoredFramesClear
        );
      }

      i++;
    };

    while (i < proposal.length || j < response.length) {
      if (i < proposal.length && j < response.length) {
        if (response[j].startTime <= proposal[i].startTime) {
          incrementResponse();
        } else {
          incrementProposal();
        }
      } else if (i < proposal.length) {
        incrementProposal();
      } else {
        incrementResponse();
      }
    }
  }
}

export class ProposalResponseWrapper {
  constructor(
    public response: IScheduleProposalResponseProcessed,
    public colorModel: IColorModel
  ) {}
}
