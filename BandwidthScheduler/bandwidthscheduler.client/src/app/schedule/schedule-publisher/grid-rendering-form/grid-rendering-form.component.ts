import { Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-grid-rendering-form',
  templateUrl: './grid-rendering-form.component.html',
  styleUrl: './grid-rendering-form.component.scss',
})
export class GridRenderingFormComponent {
  @Output() FormModel: EventEmitter<number> = new EventEmitter<number>();

  constructor() {}

  public Submit(form: NgForm) {
    const maxEmployees = form.value.maxEmployees as number;

    this.FormModel.emit(maxEmployees);
  }
}
