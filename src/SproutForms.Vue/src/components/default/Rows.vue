<script setup lang="ts">
import { isFieldVisible, type FormClientRow } from '@sproutforms/client';
import { useSproutFormContext } from '../../context';
import { useFieldScope } from '../../scope';

defineProps<{
    // A page's rows, or those of a field group's entry
    rows: FormClientRow[];
}>();

const form = useSproutFormContext();
const scope = useFieldScope();
const Field = form.resolveComponent('Field');
</script>

<template>
    <div v-for="(row, rowIndex) in rows" :key="rowIndex" class="form-row">
        <!-- A column hides with its field, so it doesn't take up space -->
        <div
            v-for="column in row.columns"
            :key="column.field.alias"
            :class="['form-col', `col-${column.width}`]"
            data-sf-col
            :hidden="!isFieldVisible(column.field, scope.values.value, form.state.value.variables) || undefined"
        >
            <component :is="Field" :field="column.field" />
        </div>
    </div>
</template>
