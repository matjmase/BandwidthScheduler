import { Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { GridRenderingFormModel } from './grid-rendering-form-model';

@Component({
  selector: 'app-grid-rendering-form',
  templateUrl: './grid-rendering-form.component.html',
  styleUrl: './grid-rendering-form.component.scss',
})
export class GridRenderingFormComponent {
  @Output() FormModel: EventEmitter<GridRenderingFormModel> =
    new EventEmitter<GridRenderingFormModel>();

  constructor() {}

  public Submit(form: NgForm) {
    const model: GridRenderingFormModel = {
      maxEmployees: form.value.maxEmployees as number,
    };

    this.FormModel.emit(model);
  }
}
