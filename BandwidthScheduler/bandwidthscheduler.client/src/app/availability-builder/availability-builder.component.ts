import { Component, OnInit } from '@angular/core';
import { BackendConnectService } from '../services/backend-connect.service';
import { AvailabilityEntry } from '../models/IAvailabilityEntry';

@Component({
  selector: 'app-availability-builder',
  templateUrl: './availability-builder.component.html',
  styleUrl: './availability-builder.component.scss',
})
export class AvailabilityBuilderComponent {
  private timeSpan = 30;
  private totalMinutes = 24 * 60;

  private totalBlock = this.totalMinutes / this.timeSpan;

  private _currentDate: Date | undefined;
  public loading: boolean = false;

  get currentDate(): Date | undefined {
    return this._currentDate;
  }
  set currentDate(date: Date) {
    this._currentDate = date;
    this.currentAvailableModels = [];
    this.loading = true;
    this.backEnd.Availability.GetAllTimes(this._currentDate).subscribe({
      complete: () => (this.loading = false),
      next: (value) => {
        this.currentAvailabilities = value.map((e) => new AvailabilityEntry(e));

        // fill out time blocks
        for (let i = 0; i < this.totalBlock; i++) {
          const startTime = this.GetDateTransformed(i);
          const endTime = this.GetDateTransformed(i + 1);
          this.currentAvailableModels.push({
            startTime: startTime,
            endTime: endTime,
            isSelected: this.currentAvailabilities.some(
              (e) =>
                // any intersection
                (e.startTime >= startTime && e.startTime < endTime) ||
                (e.endTime > startTime && e.endTime <= endTime)
            ),
          });
        }
      },
    });
  }

  currentAvailabilities: AvailabilityEntry[] = [];
  currentAvailableModels: IAvailabilityEntryModel[] = [];

  constructor(private backEnd: BackendConnectService) {}

  public GetDateTransformed(increment: number): Date {
    const cloneDate = new Date(this.currentDate!);

    cloneDate.setMinutes(this.timeSpan * increment);

    return cloneDate;
  }

  public OnSubmit(): void {
    console.log(this.currentAvailableModels.filter((e) => e.isSelected));

    this.backEnd.Availability.PutAllTimes(
      this._currentDate!,
      this.currentAvailableModels
        .filter((e) => e.isSelected)
        .map<AvailabilityEntry>((e) => {
          return {
            startTime: e.startTime,
            endTime: e.endTime,
          };
        })
    ).subscribe({
      complete: () => (this.currentAvailableModels = []),
      error: () => {},
    });
  }
}

export interface IAvailabilityEntryModel extends AvailabilityEntry {
  startTime: Date;
  endTime: Date;
  isSelected: boolean;
}
